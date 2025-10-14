namespace Api.DTOs.Requests
{
    public class GetMatchesByVenueRequestDto
    {
        public DateTime? MinDate { get; set; }

        public DateTime? MaxDate { get; set; }

        public IEnumerable<int?>? Competitions { get; set; }

        public IEnumerable<int?>? AgeGroups { get; set; }


        public bool HasValidDateRange => MinDate.HasValue && MaxDate.HasValue;

        public bool IsDateRangeValid()
        {
            if (!HasValidDateRange) { return true; }
            return MinDate <= MaxDate;
        }
    }
}
