namespace Api.DTOs.Requests
{
    public class GetMatchesRequestDto
    {
        public double? MinLat { get; set; }

        public double? MaxLat { get; set; }

        public double? MinLng { get; set; }

        public double? MaxLng { get; set; }

        public bool HasValidBoundingBox => MinLat.HasValue && MaxLat.HasValue && MinLng.HasValue && MaxLng.HasValue;

        public bool IsBoundingBoxValid()
        {
            if (!HasValidBoundingBox) { return true; } // If no bounding box provided, it's valid

            return MinLat <= MaxLat && MinLng <= MaxLng &&
                   MinLat >= -90 && MaxLat <= 90 &&
                   MinLng >= -180 && MaxLng <= 180;
        }
    }
}
