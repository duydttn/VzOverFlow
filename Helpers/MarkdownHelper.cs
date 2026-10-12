using Markdig;

namespace VzOverFlow.Helpers
{
    public static class MarkdownHelper
    {
        private static readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseBootstrap()
            .UseSoftlineBreakAsHardlineBreak()
            .DisableHtml()
            .Build();

        public static string ToHtml(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown)) return string.Empty;

            // Use Markdig for professional-grade rendering
            return Markdown.ToHtml(markdown, _pipeline);
        }
    }
}