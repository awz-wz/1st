using PIWebAPIApp.Interfaces;
using PIWebAPIApp.Models;
using PIWebAPIApp.Services;
using PIWebAPIApp.Utilities;

namespace PIWebAPIApp.Services
{
    public class PITagService : ITagService
    {
        private readonly PIWebApiClient _client;
        private static readonly List<string> _allTags = GetAllTagsList(); // Кэшируем один раз

        public PITagService(PIWebApiClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<bool> ChangeDigitalStateAsync(string dataServerWebId, string tagName, string newState)
        {
            if (string.IsNullOrWhiteSpace(dataServerWebId))
                throw new ArgumentException("Data server WebId cannot be null or empty", nameof(dataServerWebId));
           
            if (string.IsNullOrWhiteSpace(tagName))
                throw new ArgumentException("Tag name cannot be null or empty", nameof(tagName));
           
            if (string.IsNullOrWhiteSpace(newState))
                throw new ArgumentException("New state cannot be null or empty", nameof(newState));

            try
            {
                Logger.Info($"Changing digital state for tag '{tagName}' to '{newState}'");

                var point = await _client.FindPointAsync(dataServerWebId, tagName);
                if (point == null)
                {
                    Logger.Error($"Tag '{tagName}' not found");
                    return false;
                }

                Logger.Info($"Found tag: {point}");

                var currentValue = await _client.ReadValueAsync(point.WebId);
                Logger.Info($"Current value: {currentValue}");

                var writeSuccess = await _client.WriteValueAsync(point.WebId, newState);
               
                if (writeSuccess)
                {
                    Logger.Info($"Successfully changed {tagName} to {newState}");
                   
                    await Task.Delay(1000);
                   
                    var updatedValue = await _client.ReadValueAsync(point.WebId);
                    Logger.Info($"Updated value: {updatedValue}");
                   
                    return true;
                }
                else
                {
                    Logger.Error($"Failed to change {tagName} to {newState}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error changing digital state for '{tagName}': {ex.Message}");
                return false;
            }
        }

        public async Task<PIValue?> GetTagValueAsync(string dataServerWebId, string tagName)
        {
            try
            {
                var point = await _client.FindPointAsync(dataServerWebId, tagName);
                if (point == null)
                {
                    Logger.Error($"Tag '{tagName}' not found");
                    return null;
                }

                return await _client.ReadValueAsync(point.WebId);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting tag value for '{tagName}': {ex.Message}");
                throw;
            }
        }

        public async Task<bool> SetTagValueAsync(string dataServerWebId, string tagName, object value)
        {
            try
            {
                var point = await _client.FindPointAsync(dataServerWebId, tagName);
                if (point == null)
                {
                    Logger.Error($"Tag '{tagName}' not found");
                    return false;
                }

                return await _client.WriteValueAsync(point.WebId, value);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error setting tag value for '{tagName}': {ex.Message}");
                return false;
            }
        }

        public Task<List<string>> SearchTagsAsync(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter) || filter.Length < 2)
            {
                return Task.FromResult(new List<string>());
            }

            var filteredTags = _allTags
                .Where(tag => tag.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .OrderBy(tag => tag)
                .Take(20)
                .ToList();

            return Task.FromResult(filteredTags);
        }

        public List<string> GetAllTags()
        {
            return new List<string>(_allTags);
        }

        private static List<string> GetAllTagsList()
        {
            return new List<string>
            {
                "KTL-FWS-105.MV",
                "KTL-FWS-54.MV",
                "KTL-FWS-K-001-A.MV",
                "KTL-FWS-K-001-B.MV",
                "KTL-FWS-K-001-C.MV",
                "KTL-FWS-K-002-A.MV",
                "KTL-FWS-K-002-B.MV",
                "KTL-FWS-K-002-C.MV",
                "KTL-FWS-K-003.MV",
                "KTL-FWS-K-003-A.MV",
                "KTL-FWS-K-003-B.MV",
                "KTL-FWS-K-004-A.MV",
                "KTL-FWS-K-004-B.MV",
                "KTL-FWS-K-004-C.MV",
                "KTL-FWS-K-005-A.MV",
                "KTL-FWS-K-005-B.MV",
                "KTL-FWS-K-005-C.MV",
                "KTL-FWS-K-006.MV",
                "KTL-FWS-K-006-A.MV",
                "KTL-FWS-K-006-B.MV",
                "KTL-FWS-K-006-C.MV",
                "KTL-FWS-K-007-A.MV",
                "KTL-FWS-K-007-B.MV",
                "KTL-FWS-K-007-C.MV",
                "KTL-FWS-K-008-A.MV",
                "KTL-FWS-K-008-B.MV",
                "KTL-FWS-K-008-C.MV",
                "KTL-FWS-K-009.MV",
                "KTL-FWS-K-010.MV",
                "KTL-FWS-K-011.MV",
                "KTL-FWS-K-012-A.MV",
                "KTL-FWS-K-012-B.MV",
                "KTL-FWS-K-012-C.MV",
                "KTL-FWS-K-013-A.MV",
                "KTL-FWS-K-013-B.MV",
                "KTL-FWS-K-014.MV",
                "KTL-FWS-K-015-A.MV",
                "KTL-FWS-K-015-B.MV",
                "KTL-FWS-K-016.MV",
                "KTL-FWS-K-017-A.MV",
                "KTL-FWS-K-017-B.MV",
                "KTL-FWS-K-018.MV",
                "KTL-FWS-K-018-A.MV",
                "KTL-FWS-K-018-B.MV",
                "KTL-FWS-K-019.MV",
                "KTL-FWS-K-019-A.MV",
                "KTL-FWS-K-019-B.MV",
                "KTL-FWS-K-019-C.MV",
                "KTL-FWS-K-020-A.MV",
                "KTL-FWS-K-020-B.MV",
                "KTL-FWS-K-020-C.MV",
                "KTL-FWS-K-021.MV",
                "KTL-FWS-K-022.MV",
                "KTL-FWS-K-022-A.MV",
                "KTL-FWS-K-022-B.MV",
                "KTL-FWS-K-023.MV",
                "KTL-FWS-K-024-A.MV",
                "KTL-FWS-K-024-B.MV",
                "KTL-FWS-K-024-C.MV",
                "KTL-FWS-K-025.MV",
                "KTL-FWS-K-026-A.MV",
                "KTL-FWS-K-026-B.MV",
                "KTL-FWS-K-026-C.MV",
                "KTL-FWS-K-027-A.MV",
                "KTL-FWS-K-027-B.MV",
                "KTL-FWS-K-027-C.MV",
                "KTL-FWS-K-028.MV",
                "KTL-FWS-K-028-A.MV",
                "KTL-FWS-K-028-B.MV",
                "KTL-FWS-K-028-C.MV",
                "KTL-FWS-K-029.MV",
                "KTL-FWS-K-030.MV",
                "KTL-FWS-K-031.MV",
                "KTL-FWS-K-032.MV",
                "KTL-FWS-K-033-A.MV",
                "KTL-FWS-K-033-B.MV",
                "KTL-FWS-K-034.MV",
                "KTL-FWS-K-034-A.MV",
                "KTL-FWS-K-034-B.MV",
                "KTL-FWS-K-034-C.MV",
                "KTL-FWS-K-035.MV",
                "KTL-FWS-K-036.MV",
                "KTL-FWS-K-037-A.MV",
                "KTL-FWS-K-037-B.MV",
                "KTL-FWS-K-038-A.MV",
                "KTL-FWS-K-038-B.MV",
                "KTL-FWS-K-039-A.MV",
                "KTL-FWS-K-039-B.MV",
                "KTL-FWS-K-040.MV",
                "KTL-FWS-K-041.MV",
                "KTL-FWS-K-042.MV",
                "KTL-FWS-K-043-A.MV",
                "KTL-FWS-K-043-B.MV",
                "KTL-FWS-K-043-C.MV",
                "KTL-FWS-K-044-A.MV",
                "KTL-FWS-K-044-B.MV",
                "KTL-FWS-K-044-C.MV",
                "KTL-FWS-K-045 D.MV",
                "KTL-FWS-K-045-A.MV",
                "KTL-FWS-K-045-B.MV",
                "KTL-FWS-K-045-C.MV",
                "KTL-FWS-K-046.MV",
                "KTL-FWS-K-047.MV",
                "KTL-FWS-K-048.MV",
                "KTL-FWS-K-048-A.MV",
                "KTL-FWS-K-048-B.MV",
                "KTL-FWS-K-048-C.MV",
                "KTL-FWS-K-049.MV",
                "KTL-FWS-K-050.MV",
                "KTL-FWS-K-050-A.MV",
                "KTL-FWS-K-050-B.MV",
                "KTL-FWS-K-051-A.MV",
                "KTL-FWS-K-051-B.MV",
                "KTL-FWS-K-051-C.MV",
                "KTL-FWS-K-052.MV",
                "KTL-FWS-K-053.MV",
                "KTL-FWS-K-054.MV",
                "KTL-FWS-K-055.MV",
                "KTL-FWS-K-056.MV",
                "KTL-FWS-K-057.MV",
                "KTL-FWS-K-058.MV",
                "KTL-FWS-K-059.MV",
                "KTL-FWS-K-060.MV",
                "KTL-FWS-K-061.MV",
                "KTL-FWS-K-062.MV",
                "KTL-FWS-K-063.MV",
                "KTL-FWS-K-064.MV",
                "KTL-FWS-K-065.MV",
                "KTL-FWS-K-066.MV",
                "KTL-FWS-K-067.MV",
                "KTL-FWS-K-068.MV",
                "KTL-FWS-K-069.MV",
                "KTL-FWS-K-070.MV",
                "KTL-FWS-K-071.MV",
                "KTL-FWS-K-072.MV",
                "KTL-FWS-K-073.MV",
                "KTL-FWS-K-074.MV",
                "KTL-FWS-K-075.MV",
                "KTL-FWS-K-076.MV",
                "KTL-FWS-K-077.MV",
                "KTL-FWS-K-078.MV",
                "KTL-FWS-K-079.MV",
                "KTL-FWS-K-080.MV",
                "KTL-FWS-K-081.MV",
                "KTL-FWS-K-082.MV",
                "KTL-FWS-K-083.MV",
                "KTL-FWS-K-084.MV",
                "KTL-FWS-K-085.MV",
                "KTL-FWS-K-086.MV",
                "KTL-FWS-K-087.MV",
                "KTL-FWS-K-087-A.MV",
                "KTL-FWS-K-087-B.MV",
                "KTL-FWS-K-089.MV",
                "KTL-FWS-K-090.MV",
                "KTL-FWS-K-091.MV",
                "KTL-FWS-K-092.MV",
                "KTL-FWS-K-093.MV",
                "KTL-FWS-K-094.MV",
                "KTL-FWS-K-095.MV",
                "KTL-FWS-K-096.MV",
                "KTL-FWS-K-097.MV",
                "KTL-FWS-K-098.MV",
                "KTL-FWS-K-098-A.MV",
                "KTL-FWS-K-098-B.MV",
                "KTL-FWS-K-099.MV",
                "KTL-FWS-K-100.MV",
                "KTL-FWS-K-101.MV",
                "KTL-FWS-K-102.MV",
                "KTL-FWS-K-103.MV",
                "KTL-FWS-K-104.MV",
                "KTL-FWS-K-105.MV",
                "KTL-FWS-K-106.MV",
                "KTL-FWS-K-107.MV",
                "KTL-FWS-K-107-A.MV",
                "KTL-FWS-K-107-B.MV",
                "KTL-FWS-K-108.MV",
                "KTL-FWS-K-109.MV",
                "KTL-FWS-K-110.MV",
                "KTL-FWS-K-111.MV",
                "KTL-FWS-K-111-A.MV",
                "KTL-FWS-K-111-B.MV",
                "KTL-FWS-K-112.MV",
                "KTL-FWS-K-113.MV",
                "KTL-FWS-K-114.MV",
                "KTL-FWS-K-115.MV",
                "KTL-FWS-K-116.MV",
                "KTL-FWS-K-117.MV",
                "KTL-FWS-K-119.MV",
                "KTL-FWS-K-120.MV",
                "KTL-FWS-K-121.MV",
                "KTL-FWS-K-121-A.MV",
                "KTL-FWS-K-121-B.MV",
                "KTL-FWS-K-122.MV",
                "KTL-FWS-K-123.MV",
                "KTL-FWS-K-124.MV",
                "KTL-FWS-K-125-A.MV",
                "KTL-FWS-K-125-B.MV",
                "KTL-FWS-K-125-C.MV",
                "KTL-FWS-K-126.MV",
                "KTL-FWS-K-127.MV",
                "KTL-FWS-K-128.MV",
                "KTL-FWS-K-130.MV",
                "KTL-FWS-K-131-A.MV",
                "KTL-FWS-K-131-B.MV",
                "KTL-FWS-K-131-C.MV",
                "KTL-FWS-K-132.MV",
                "KTL-FWS-K-132-F.MV",
                "KTL-FWS-K-133.MV",
                "KTL-FWS-K-134.MV",
                "KTL-FWS-K-134-A.MV",
                "KTL-FWS-K-134-B1.MV",
                "KTL-FWS-K-134-B2.MV",
                "KTL-FWS-K-134-B3.MV",
                "KTL-FWS-K-134-B4.MV",
                "KTL-FWS-K-134-C.MV",
                "KTL-FWS-K-135.MV",
                "KTL-FWS-K-136.MV",
                "KTL-FWS-K-137.MV",
                "KTL-FWS-K-138.MV",
                "KTL-FWS-K-139.MV",
                "KTL-FWS-K-140.MV",
                "KTL-FWS-K-141.MV",
                "KTL-FWS-K-142.MV",
                "KTL-FWS-K-143.MV",
                "KTL-FWS-K-144.MV",
                "KTL-FWS-K-147.MV",
                "KTL-FWS-K-147-A.MV",
                "KTL-FWS-K-147-B.MV",
                "KTL-FWS-K-153.MV",
                "KTL-FWS-K-155.MV",
                "KTL-FWS-K-156.MV",
                "KTL-FWS-K-157.MV",
                "KTL-FWS-K-158.MV",
                "KTL-FWS-K-180.MV",
                "KTL-FWS-K-181.MV",
                "KTL-FWS-K-208-A.MV",
                "KTL-FWS-K-208-B.MV",
                "KTL-FWS-K-211.MV",
                "KTL-FWS-K-213.MV",
                "KTL-FWS-K-215-A.MV",
                "KTL-FWS-K-215-B.MV",
                "KTL-FWS-K-216.MV",
                "KTL-FWS-K-217-A.MV",
                "KTL-FWS-K-217-B.MV",
                "KTL-FWS-K-218.MV",
                "KTL-FWS-K-219.MV",
                "KTL-FWS-K-220.MV",
                "KTL-FWS-K-220-A1.MV",
                "KTL-FWS-K-220-A2.MV",
                "KTL-FWS-K-220-B1.MV",
                "KTL-FWS-K-220-B2.MV",
                "KTL-FWS-K-232.MV",
                "KTL-FWS-K-232-A.MV",
                "KTL-FWS-K-232-B.MV",
                "KTL-FWS-K-234.MV",
                "KTL-FWS-K-235.MV",
                "KTL-FWS-K-236.MV",
                "KTL-FWS-K-237.MV",
                "KTL-FWS-K-238.MV",
                "KTL-FWS-K-239.MV",
                "KTL-FWS-K-241.MV",
                "KTL-FWS-K-245.MV",
                "KTL-FWS-K-246.MV",
                "KTL-FWS-K-248-A.MV",
                "KTL-FWS-K-248-B.MV",
                "KTL-FWS-K-249.MV",
                "KTL-FWS-K-251.MV",
                "KTL-FWS-K-252.MV",
                "KTL-FWS-K-253.MV",
                "KTL-FWS-K-254.MV",
                "KTL-FWS-K-257.MV",
                "KTL-FWS-K-258.MV",
                "KTL-FWS-K-259.MV",
                "KTL-FWS-K-260.MV",
                "KTL-FWS-K-261-A.MV",
                "KTL-FWS-K-261-B.MV",
                "KTL-FWS-K-263.MV",
                "KTL-FWS-K-264.MV",
                "KTL-FWS-K-267.MV",
                "KTL-FWS-K-268.MV",
                "KTL-FWS-K-269.MV",
                "KTL-FWS-K-270.MV",
                "KTL-FWS-K-271.MV",
                "KTL-FWS-K-272.MV",
                "KTL-FWS-K-329.MV",
                "KTL-FWS-K-345.MV",
                "KTL-FWS-K-346.MV",
                "KTL-FWS-K-349.MV",
                "KTL-FWS-K-350.MV",
                "KTL-FWS-K-351.MV",
                "KTL-FWS-K-352.MV",
                "KTL-FWS-K-353.MV",
                "KTL-FWS-K-356.MV",
                "KTL-FWS-K-359.MV",
                "KTL-FWS-K-360.MV",
                "KTL-FWS-K-361.MV",
                "KTL-FWS-K-362.MV",
                "KTL-FWS-K-363.MV",
                "KTL-FWS-K-364.MV",
                "KTL-FWS-K-365.MV",
                "KTL-FWS-K-390.MV",
                "KTL-FWS-K-391-A.MV",
                "KTL-FWS-K-391-B.MV",
                "KTL-FWS-VP-1-A.MV",
                "KTL-FWS-VP-1-B.MV"
            };
        }
    }
}