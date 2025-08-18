using System.Text.Json;

namespace PIWebAPIApp.Models
{
    public class PIPoint
    {
        public string WebId { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string Descriptor { get; set; }
        public string PointClass { get; set; }
        public string PointType { get; set; }

        public override string ToString()
        {
            return $"PIPoint: {Name} (WebId: {WebId})";
        }
    }
}