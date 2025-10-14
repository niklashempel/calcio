/**
 * Example: Filter Panel Component
 * 
 * This is a simple example of how to create a UI component that uses the filter URL parameters.
 * This component can be added to MapControl or any other component to provide filter controls.
 */

import { useMatches } from '@/hooks/useMatches';
import { useState, useEffect } from 'react';

interface FilterPanelProps {
  // Optional: Pass available competitions and age groups from an API call
  availableCompetitions?: Array<{ id: number; name: string }>;
  availableAgeGroups?: Array<{ id: number; name: string }>;
}

function FilterPanel({ availableCompetitions = [], availableAgeGroups = [] }: FilterPanelProps) {
  const { filters, updateFilters } = useMatches();
  
  const [minDate, setMinDate] = useState(filters.minDate || '');
  const [maxDate, setMaxDate] = useState(filters.maxDate || '');
  const [selectedCompetitions, setSelectedCompetitions] = useState<number[]>(filters.competitions || []);
  const [selectedAgeGroups, setSelectedAgeGroups] = useState<number[]>(filters.ageGroups || []);

  // Sync local state with filters from URL
  useEffect(() => {
    setMinDate(filters.minDate || '');
    setMaxDate(filters.maxDate || '');
    setSelectedCompetitions(filters.competitions || []);
    setSelectedAgeGroups(filters.ageGroups || []);
  }, [filters]);

  const handleApplyFilters = () => {
    updateFilters({
      minDate: minDate || undefined,
      maxDate: maxDate || undefined,
      competitions: selectedCompetitions.length > 0 ? selectedCompetitions : undefined,
      ageGroups: selectedAgeGroups.length > 0 ? selectedAgeGroups : undefined,
    });
  };

  const handleClearFilters = () => {
    setMinDate('');
    setMaxDate('');
    setSelectedCompetitions([]);
    setSelectedAgeGroups([]);
    updateFilters({});
  };

  const toggleCompetition = (id: number) => {
    setSelectedCompetitions(prev =>
      prev.includes(id) ? prev.filter(c => c !== id) : [...prev, id]
    );
  };

  const toggleAgeGroup = (id: number) => {
    setSelectedAgeGroups(prev =>
      prev.includes(id) ? prev.filter(a => a !== id) : [...prev, id]
    );
  };

  return (
    <div className="filter-panel">
      <h3>Filter Matches</h3>
      
      <div className="filter-section">
        <h4>Date Range</h4>
        <label>
          From:
          <input
            type="date"
            value={minDate}
            onChange={(e) => setMinDate(e.target.value)}
          />
        </label>
        <label>
          To:
          <input
            type="date"
            value={maxDate}
            onChange={(e) => setMaxDate(e.target.value)}
          />
        </label>
      </div>

      {availableCompetitions.length > 0 && (
        <div className="filter-section">
          <h4>Competitions</h4>
          {availableCompetitions.map(comp => (
            <label key={comp.id}>
              <input
                type="checkbox"
                checked={selectedCompetitions.includes(comp.id)}
                onChange={() => toggleCompetition(comp.id)}
              />
              {comp.name}
            </label>
          ))}
        </div>
      )}

      {availableAgeGroups.length > 0 && (
        <div className="filter-section">
          <h4>Age Groups</h4>
          {availableAgeGroups.map(age => (
            <label key={age.id}>
              <input
                type="checkbox"
                checked={selectedAgeGroups.includes(age.id)}
                onChange={() => toggleAgeGroup(age.id)}
              />
              {age.name}
            </label>
          ))}
        </div>
      )}

      <div className="filter-actions">
        <button onClick={handleApplyFilters}>Apply Filters</button>
        <button onClick={handleClearFilters}>Clear All</button>
      </div>
    </div>
  );
}

export default FilterPanel;

/**
 * Usage in MapControl:
 * 
 * import FilterPanel from './FilterPanel';
 * 
 * function MapControl() {
 *   // ... existing code ...
 *   
 *   return (
 *     <div className="map-container">
 *       <FilterPanel />
 *       <LeafletMap ... />
 *       ...
 *     </div>
 *   );
 * }
 * 
 * The filters will automatically:
 * 1. Be synced to the URL as query parameters
 * 2. Be passed to the API when loading matches
 * 3. Allow sharing filtered views via URL
 * 4. Support browser back/forward navigation
 */
