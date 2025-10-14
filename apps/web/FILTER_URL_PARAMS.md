# Filter URL Parameters

This document describes how to use the filter URL parameters feature.

## Overview

The web application now supports filter parameters that are automatically synchronized with the browser URL. This enables:

- **Shareable filtered views**: Share specific filter configurations via URL
- **Browser history support**: Use back/forward buttons to navigate between filter states
- **Deep linking**: Link directly to specific filter configurations

## URL Parameters

The following filter parameters can be added to the URL:

### Date Filters
- `minDate` - Minimum date for matches (ISO format: YYYY-MM-DD)
- `maxDate` - Maximum date for matches (ISO format: YYYY-MM-DD)

### Category Filters
- `competitions` - Competition IDs (can be specified multiple times)
- `ageGroups` - Age group IDs (can be specified multiple times)

## Examples

### Filter by date range
```
/?minDate=2024-01-01&maxDate=2024-12-31
```

### Filter by specific competitions
```
/?competitions=1&competitions=2&competitions=3
```

### Filter by age groups
```
/?ageGroups=10&ageGroups=20
```

### Combine multiple filters
```
/?minDate=2024-06-01&maxDate=2024-08-31&competitions=1&ageGroups=10
```

## Programmatic Usage

### Reading filters from URL

```typescript
import { readFiltersFromUrl } from '@/utils/urlParams';

// Get current filters from URL
const filters = readFiltersFromUrl();
// Returns: { minDate?: string, maxDate?: string, competitions?: number[], ageGroups?: number[] }
```

### Writing filters to URL

```typescript
import { writeFiltersToUrl } from '@/utils/urlParams';

// Update URL with new filters
writeFiltersToUrl({
  minDate: '2024-01-01',
  maxDate: '2024-12-31',
  competitions: [1, 2, 3],
  ageGroups: [10]
});
```

### Using with useMatches hook

```typescript
import { useMatches } from '@/hooks/useMatches';

function MyComponent() {
  const { matches, loading, load, filters, updateFilters } = useMatches();

  // Update filters programmatically
  const handleFilterChange = () => {
    updateFilters({
      minDate: '2024-06-01',
      competitions: [1, 2]
    });
  };

  // Filters are automatically synced to URL
  // Load function automatically includes current filters
}
```

## Implementation Details

- Filters are stored in the React state and automatically synced to URL
- URL updates use `pushState` for seamless navigation without page reload
- Empty filter values are removed from the URL
- Invalid values (e.g., non-numeric IDs) are automatically filtered out
- The implementation is backward compatible - existing functionality works without changes
