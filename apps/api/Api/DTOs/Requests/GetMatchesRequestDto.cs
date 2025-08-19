namespace Api.DTOs.Requests
{
    public class GetMatchesRequestDto
    {
        public double? MinLat { get; set; }

        public double? MaxLat { get; set; }

        public double? MinLng { get; set; }

        public double? MaxLng { get; set; }

        public bool HasValidBoundingBox => MinLat.HasValue && MaxLat.HasValue && MinLng.HasValue && MaxLng.HasValue;
    }
}
