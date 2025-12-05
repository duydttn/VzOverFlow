using Microsoft.EntityFrameworkCore;
using VzOverFlow.Data;
using VzOverFlow.Models;

namespace VzOverFlow.Services
{
    public interface IGamificationService
    {
        Task<int> AwardXpAsync(int userId, ActivityType activityType, int? questionId = null, int? answerId = null);
 Task CheckAndAwardBadgesAsync(int userId);
        Task<DailyMission> GetTodayMissionAsync(int userId);
  Task UpdateDailyMissionAsync(int userId, ActivityType activityType, int xpEarned);
    }

    public class GamificationService : IGamificationService
    {
        private readonly AppDbContext _context;

// XP rewards for each activity
  private readonly Dictionary<ActivityType, int> _xpRewards = new()
{
            { ActivityType.AskQuestion, 10 },
   { ActivityType.AnswerQuestion, 15 },
{ ActivityType.VoteUp, 1 },
   { ActivityType.VoteDown, 1 },
 { ActivityType.AcceptAnswer, 5 },
   { ActivityType.QuestionUpvoted, 5 },
    { ActivityType.AnswerUpvoted, 10 },
 { ActivityType.AnswerAccepted, 25 }
   };

   public GamificationService(AppDbContext context)
 {
      _context = context;
  }

     public async Task<int> AwardXpAsync(int userId, ActivityType activityType, int? questionId = null, int? answerId = null)
  {
if (!_xpRewards.TryGetValue(activityType, out int xpAmount))
   {
  xpAmount = 0;
       }

     if (xpAmount == 0) return 0;

       var user = await _context.Users.FindAsync(userId);
      if (user == null) return 0;

      // Award XP
     user.ExperiencePoints += xpAmount;

    // Log activity
       var activity = new UserActivity
    {
  UserId = userId,
      Type = activityType,
        QuestionId = questionId,
        AnswerId = answerId,
   XpEarned = xpAmount,
   CreatedAt = DateTime.UtcNow
      };

   _context.UserActivities.Add(activity);
  await _context.SaveChangesAsync();

   // Update daily mission
    await UpdateDailyMissionAsync(userId, activityType, xpAmount);

// Check for new badges
 await CheckAndAwardBadgesAsync(userId);

  return xpAmount;
    }

 public async Task CheckAndAwardBadgesAsync(int userId)
     {
       var user = await _context.Users
 .Include(u => u.Badges)
     .Include(u => u.Questions)
    .Include(u => u.Answers)
  .Include(u => u.Votes)
   .FirstOrDefaultAsync(u => u.Id == userId);

    if (user == null) return;

     var existingBadges = user.Badges.Select(b => b.Badge).ToHashSet();
    var newBadges = new List<BadgeType>();

  // Question badges
     if (user.Questions.Count >= 1 && !existingBadges.Contains(BadgeType.Curious))
      newBadges.Add(BadgeType.Curious);
      if (user.Questions.Count >= 5 && !existingBadges.Contains(BadgeType.Inquisitive))
       newBadges.Add(BadgeType.Inquisitive);
     if (user.Questions.Count >= 50 && !existingBadges.Contains(BadgeType.Questioner))
     newBadges.Add(BadgeType.Questioner);

    // Answer badges
     if (user.Answers.Count >= 1 && !existingBadges.Contains(BadgeType.Teacher))
    newBadges.Add(BadgeType.Teacher);
  if (user.Answers.Count >= 10 && !existingBadges.Contains(BadgeType.Educator))
       newBadges.Add(BadgeType.Educator);
 if (user.Answers.Count >= 50 && !existingBadges.Contains(BadgeType.Scholar))
      newBadges.Add(BadgeType.Scholar);

    // Vote badges
    if (user.Votes.Count >= 10 && !existingBadges.Contains(BadgeType.Supporter))
   newBadges.Add(BadgeType.Supporter);
  if (user.Votes.Count >= 100 && !existingBadges.Contains(BadgeType.Critic))
      newBadges.Add(BadgeType.Critic);
if (user.Votes.Count >= 500 && !existingBadges.Contains(BadgeType.Judge))
  newBadges.Add(BadgeType.Judge);

       // Check for answer quality badges
   var answerUpvotes = await _context.Votes
    .Where(v => v.AnswerId != null && 
           user.Answers.Select(a => a.Id).Contains(v.AnswerId.Value) && 
  v.Value > 0)
   .GroupBy(v => v.AnswerId)
   .Select(g => g.Count())
.ToListAsync();

    if (answerUpvotes.Any(count => count >= 10) && !existingBadges.Contains(BadgeType.NiceAnswer))
     newBadges.Add(BadgeType.NiceAnswer);
       if (answerUpvotes.Any(count => count >= 25) && !existingBadges.Contains(BadgeType.GoodAnswer))
  newBadges.Add(BadgeType.GoodAnswer);
         if (answerUpvotes.Any(count => count >= 50) && !existingBadges.Contains(BadgeType.GreatAnswer))
      newBadges.Add(BadgeType.GreatAnswer);

        // Check for accepted answer
 if (user.Answers.Any(a => a.IsAccepted) && !existingBadges.Contains(BadgeType.Accepted))
      newBadges.Add(BadgeType.Accepted);

// Award new badges
   foreach (var badgeType in newBadges)
 {
      _context.UserBadges.Add(new UserBadge
    {
UserId = userId,
     Badge = badgeType,
   EarnedAt = DateTime.UtcNow
   });
   }

  if (newBadges.Any())
       {
await _context.SaveChangesAsync();
     }
   }

   public async Task<DailyMission> GetTodayMissionAsync(int userId)
 {
 var today = DateTime.UtcNow.Date;

        var mission = await _context.DailyMissions
  .FirstOrDefaultAsync(d => d.UserId == userId && d.Date == today);

      if (mission == null)
   {
     mission = new DailyMission
    {
  UserId = userId,
      Date = today
   };
 _context.DailyMissions.Add(mission);
 await _context.SaveChangesAsync();
            }

  return mission;
        }

    public async Task UpdateDailyMissionAsync(int userId, ActivityType activityType, int xpEarned)
        {
    var mission = await GetTodayMissionAsync(userId);

   switch (activityType)
 {
  case ActivityType.AskQuestion:
    mission.QuestionsAsked++;
  break;
  case ActivityType.AnswerQuestion:
  mission.AnswersGiven++;
    break;
    case ActivityType.VoteUp:
       case ActivityType.VoteDown:
  mission.VotesCast++;
     break;
         }

    mission.TotalXpToday += xpEarned;
     mission.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
  }
    }
}
