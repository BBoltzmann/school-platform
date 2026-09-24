namespace SchoolPlatform.Application.Timetabling;

/// <summary>
/// Calculates class timetable slots after collapsing configured parallel
/// subject groups into shared occurrences.
/// </summary>
public static class ParallelTimetableCapacity
{
    public static (int Raw, int Effective, int Savings) Calculate(
        IReadOnlyDictionary<Guid, int> periodsBySubject,
        IEnumerable<IReadOnlyCollection<Guid>> parallelGroups)
    {
        var raw = periodsBySubject.Values.Sum();
        var effective = raw;
        var groupedSubjects = new HashSet<Guid>();

        foreach (var members in parallelGroups)
        {
            var ids = members
                .Where(periodsBySubject.ContainsKey)
                .Distinct()
                .ToArray();

            if (ids.Length < 2 || ids.Any(groupedSubjects.Contains))
            {
                continue;
            }

            var memberPeriods = ids.Select(id => periodsBySubject[id]).ToArray();
            if (memberPeriods.Any(x => x <= 0) || memberPeriods.Distinct().Count() != 1)
            {
                continue;
            }

            effective -= memberPeriods.Sum() - memberPeriods[0];
            foreach (var id in ids)
            {
                groupedSubjects.Add(id);
            }
        }

        return (raw, effective, raw - effective);
    }
}
