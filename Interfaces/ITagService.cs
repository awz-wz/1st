using PIWebAPIApp.Models;
using PIWebAPIApp.Models.DTOs;

namespace PIWebAPIApp.Interfaces
{
    public interface ITagService
    {
        Task<PIValue?> GetTagValueAsync(string dataServerWebId, string tagName);
        Task<bool> ChangeDigitalStateAsync(string dataServerWebId, string tagName, string newState);
        Task<bool> SetTagValueAsync(string dataServerWebId, string tagName, object value);
        Task<List<string>> SearchTagsAsync(string filter);
        List<string> GetAllTags();
    }
}