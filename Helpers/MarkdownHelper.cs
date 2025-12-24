using Markdig;

namespace VzOverFlow.Helpers
{
    public static class MarkdownHelper
    {
        private static readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseBootstrap()
            .Build();
            
        public static string ToHtml(string markdown)
        {
            if (string.IsNullOrEmpty(markdown)) return string.Empty;
            return Markdown.ToHtml(markdown, _pipeline);
        }
    }
}