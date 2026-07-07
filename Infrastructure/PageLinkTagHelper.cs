using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using TMDTStore.Models.ViewModels;

namespace TMDTStore.Infrastructure;

[HtmlTargetElement("div", Attributes = "page-model")]
public class PageLinkTagHelper : TagHelper
{
    private readonly IUrlHelperFactory _urlHelperFactory;

    public PageLinkTagHelper(IUrlHelperFactory urlHelperFactory)
    {
        _urlHelperFactory = urlHelperFactory;
    }

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext? ViewContext { get; set; }

    public PagingInfo? PageModel { get; set; }
    public string? PageAction { get; set; }

    [HtmlAttributeName(DictionaryAttributePrefix = "page-url-")]
    public Dictionary<string, object> PageUrlValues { get; set; } = new();

    public bool PageClassesEnabled { get; set; }
    public string PageClass { get; set; } = string.Empty;
    public string PageClassNormal { get; set; } = string.Empty;
    public string PageClassSelected { get; set; } = string.Empty;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (ViewContext == null || PageModel == null) return;

        var urlHelper = _urlHelperFactory.GetUrlHelper(ViewContext);
        var result = new TagBuilder("div");
        result.AddCssClass("flex items-center justify-center gap-2 mt-10");

        // Nút Previous
        if (PageModel.CurrentPage > 1)
        {
            PageUrlValues["page"] = PageModel.CurrentPage - 1;
            var prevTag = new TagBuilder("a");
            prevTag.Attributes["href"] = urlHelper.Action(PageAction, PageUrlValues);
            prevTag.AddCssClass("px-3 py-2 rounded-lg text-sm font-medium bg-white text-brand-600 hover:bg-brand-50 border border-brand-200 transition-all duration-200");
            prevTag.InnerHtml.AppendHtml(
                "<svg class=\"w-4 h-4 inline\" fill=\"none\" viewBox=\"0 0 24 24\" stroke=\"currentColor\" stroke-width=\"2\">" +
                "<path stroke-linecap=\"round\" stroke-linejoin=\"round\" d=\"M15 19l-7-7 7-7\" /></svg>");
            result.InnerHtml.AppendHtml(prevTag);
        }

        for (int i = 1; i <= PageModel.TotalPages; i++)
        {
            PageUrlValues["page"] = i;
            var tag = new TagBuilder("a");
            tag.Attributes["href"] = urlHelper.Action(PageAction, PageUrlValues);
            tag.InnerHtml.Append(i.ToString());

            if (PageClassesEnabled)
            {
                tag.AddCssClass(PageClass);
                tag.AddCssClass(i == PageModel.CurrentPage ? PageClassSelected : PageClassNormal);
            }
            else
            {
                var isActive = i == PageModel.CurrentPage;
                tag.AddCssClass("px-4 py-2 rounded-lg text-sm font-medium transition-all duration-200");
                tag.AddCssClass(isActive
                    ? "bg-brand-600 text-white shadow-md"
                    : "bg-white text-brand-600 hover:bg-brand-50 border border-brand-200");
            }

            result.InnerHtml.AppendHtml(tag);
        }

        // Nút Next
        if (PageModel.CurrentPage < PageModel.TotalPages)
        {
            PageUrlValues["page"] = PageModel.CurrentPage + 1;
            var nextTag = new TagBuilder("a");
            nextTag.Attributes["href"] = urlHelper.Action(PageAction, PageUrlValues);
            nextTag.AddCssClass("px-3 py-2 rounded-lg text-sm font-medium bg-white text-brand-600 hover:bg-brand-50 border border-brand-200 transition-all duration-200");
            nextTag.InnerHtml.AppendHtml(
                "<svg class=\"w-4 h-4 inline\" fill=\"none\" viewBox=\"0 0 24 24\" stroke=\"currentColor\" stroke-width=\"2\">" +
                "<path stroke-linecap=\"round\" stroke-linejoin=\"round\" d=\"M9 5l7 7-7 7\" /></svg>");
            result.InnerHtml.AppendHtml(nextTag);
        }

        output.Content.AppendHtml(result.InnerHtml);
    }
}
