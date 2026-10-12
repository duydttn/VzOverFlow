using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using VzOverFlow.Data;
using VzOverFlow.Models;
using VzOverFlow.Services;
using VzOverFlow.Services.AIService;
using VzOverFlow.Hubs;
using VzOverFlow.Middlewares;

namespace VzOverFlow
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

            var mvcBuilder = builder.Services.AddControllersWithViews();
            if (builder.Environment.IsDevelopment())
            {
                mvcBuilder.AddRazorRuntimeCompilation();
            }

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
                {
                    options.Password.RequiredLength = 6;
                    options.Password.RequireDigit = true;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.User.RequireUniqueEmail = false;
                })
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/users/login";
                options.AccessDeniedPath = "/users/login";
            });

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            builder.Services.AddScoped<IQuestionService, QuestionService>();
            builder.Services.AddScoped<IQueryService, QueryService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<ITwoFactorService, TwoFactorService>();
            builder.Services.AddScoped<IAuthenticatorService, AuthenticatorService>();
            builder.Services.AddScoped<IEmailSender, GmailEmailSender>();
            builder.Services.AddScoped<IReportService, ReportService>();
            builder.Services.AddScoped<IGamificationService, GamificationService>();
            builder.Services.AddScoped<IVoteService, VoteService>();
            builder.Services.AddScoped<IAiConfigService, AiConfigService>();
            builder.Services.AddScoped<ICommentService, CommentService>();
            builder.Services.AddHttpClient<IGeminiService, GeminiService>();
            builder.Services.AddHttpClient<IOpenAIService, OpenAIService>();
            builder.Services.AddScoped<IReputationService, ReputationService>();
            builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
            builder.Services.AddScoped<INotificationService, NotificationService>();
            
            // OAuth Authentication Providers
            var authBuilder = builder.Services.AddAuthentication();
            
            // Google
            var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
            var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
            if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
            {
                authBuilder.AddGoogle(options =>
                {
                    options.ClientId = googleClientId;
                    options.ClientSecret = googleClientSecret;
                });
            }
            
            // Facebook
            var facebookAppId = builder.Configuration["Authentication:Facebook:AppId"];
            var facebookAppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
            if (!string.IsNullOrEmpty(facebookAppId) && !string.IsNullOrEmpty(facebookAppSecret))
            {
                authBuilder.AddFacebook(options =>
                {
                    options.AppId = facebookAppId;
                    options.AppSecret = facebookAppSecret;
                    options.Scope.Clear(); // Remove default email scope - requires app review
                    options.Scope.Add("public_profile");
                });
            }

            
            // GitHub (using OAuth)
            var githubClientId = builder.Configuration["Authentication:GitHub:ClientId"];
            var githubClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"];
            if (!string.IsNullOrEmpty(githubClientId) && !string.IsNullOrEmpty(githubClientSecret))
            {
                authBuilder.AddOAuth("GitHub", options =>
                {
                    options.ClientId = githubClientId;
                    options.ClientSecret = githubClientSecret;
                    options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
                    options.TokenEndpoint = "https://github.com/login/oauth/access_token";
                    options.UserInformationEndpoint = "https://api.github.com/user";
                    options.CallbackPath = "/signin-github";
                    options.Scope.Add("user:email");
                    options.Events.OnCreatingTicket = async context =>
                    {
                        var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, context.Options.UserInformationEndpoint);
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", context.AccessToken);
                        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                        request.Headers.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("VzOverFlow", "1.0"));
                        var response = await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted);
                        response.EnsureSuccessStatusCode();
                        var json = await response.Content.ReadAsStringAsync();
                        var user = System.Text.Json.JsonDocument.Parse(json);
                        var root = user.RootElement;
                        
                        // Manually add claims
                        var claims = new List<System.Security.Claims.Claim>();
                        if (root.TryGetProperty("id", out var id))
                            claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, id.ToString()));
                        if (root.TryGetProperty("login", out var login))
                            claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, login.GetString() ?? ""));
                        if (root.TryGetProperty("email", out var email) && email.ValueKind != System.Text.Json.JsonValueKind.Null)
                            claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, email.GetString() ?? ""));
                        if (root.TryGetProperty("avatar_url", out var avatar))
                            claims.Add(new System.Security.Claims.Claim("urn:github:avatar", avatar.GetString() ?? ""));
                        
                        context.Identity?.AddClaims(claims);
                    };
                });
            }

            // Rate Limiting
            builder.Services.AddRateLimiter(options =>
            {
                options.AddFixedWindowLimiter("fixed", opt =>
                {
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.PermitLimit = 2000;
                    opt.QueueLimit = 0;
                });
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });
            
            builder.Services.AddSignalR();

            var app = builder.Build();
            
            app.UseForwardedHeaders();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                // app.UseHsts();
            }
            
            app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

            // app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            
            app.UseRateLimiter(); // Add middleware here

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseUserBanEnforcement();

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<AppDbContext>();
                await context.Database.MigrateAsync();
                // await SeedData.InitializeAsync(services);
                var reputationService = services.GetRequiredService<IReputationService>();
                await reputationService.RecalculateAllUserReputationsAsync();
            }

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets()
                .RequireRateLimiting("fixed"); // Global Rate Limiting for Controllers
            app.MapHub<ChatHub>("/chatHub");
            app.MapHub<NotificationHub>("/notificationHub");
            await app.RunAsync();
        }
    }
}