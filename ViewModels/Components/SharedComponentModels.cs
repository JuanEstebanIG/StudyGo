// ============================================================================
// StudyGo · SharedComponentModels.cs — modelos de los partials de §5
// ----------------------------------------------------------------------------
// ⚠️ ARCHIVO COMPARTIDO (§9). Dueño oficial: MICKY.
// STAND-IN PROVISIONAL de Jaison para que los partials compartidos que consume
// el módulo de Comunicación (_Avatar, _Badge, _EmptyState, _PageHeader)
// compilen. Cuando Micky entregue los suyos, REEMPLAZA este archivo.
// ============================================================================
namespace StudyGo.ViewModels.Components
{
    public class PageHeaderModel
    {
        public string? Eyebrow { get; set; }
        public string Title { get; set; } = "";
        public string? Subtitle { get; set; }
    }

    public class AvatarModel
    {
        public string? Name { get; set; }
        public string? ImageUrl { get; set; }
        /// <summary>sm | md | lg</summary>
        public string Size { get; set; } = "md";

        public string Initials =>
            string.IsNullOrWhiteSpace(Name)
                ? "?"
                : string.Concat(Name.Trim().Split(' ', System.StringSplitOptions.RemoveEmptyEntries)
                    .Take(2).Select(p => char.ToUpper(p[0]))).PadRight(1, ' ').Trim();
    }

    public class BadgeModel
    {
        public string Text { get; set; } = "";
        /// <summary>neutral | success | info | warn | danger</summary>
        public string Variant { get; set; } = "neutral";
        public string? Icon { get; set; }
    }

    public class EmptyStateModel
    {
        public string Icon { get; set; } = "fa-regular fa-folder-open";
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string? ActionText { get; set; }
        public string? ActionHref { get; set; }
    }
}
