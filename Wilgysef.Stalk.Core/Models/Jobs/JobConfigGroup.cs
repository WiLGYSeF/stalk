using System.Collections;

namespace Wilgysef.Stalk.Core.Models.Jobs;

public class JobConfigGroupCollection : ICollection<JobConfigGroup>
{
    public int Count => _groups.Count;

    public bool IsReadOnly => false;

    private readonly Dictionary<string, JobConfigGroup> _groups;

    public JobConfigGroupCollection()
    {
        _groups = new();
    }

    public JobConfigGroupCollection(int capacity)
    {
        _groups = new(capacity);
    }

    public JobConfigGroupCollection(IEnumerable<JobConfigGroup>? groups) : this()
    {
        if (groups != null)
        {
            foreach (var group in groups)
            {
                Add(group);
            }
        }
    }

    public JobConfigGroupCollection(int capacity, IEnumerable<JobConfigGroup>? groups) : this(capacity)
    {
        if (groups != null)
        {
            foreach (var group in groups)
            {
                Add(group);
            }
        }
    }

    public void Add(JobConfigGroup item)
    {
        if (_groups.TryGetValue(item.Name, out var group))
        {
            foreach (var (key, value) in item.Config)
            {
                group.Config[key] = value;
            }
        }
        else
        {
            _groups.Add(item.Name, item);
        }
    }

    public void Clear()
    {
        _groups.Clear();
    }

    public bool Contains(JobConfigGroup item)
    {
        throw new NotSupportedException();
    }

    public bool Contains(string name)
    {
        return _groups.ContainsKey(name);
    }

    public bool Remove(JobConfigGroup item)
    {
        throw new NotSupportedException();
    }

    public bool Remove(string name)
    {
        return _groups.Remove(name);
    }

    public IDictionary<string, IDictionary<string, object?>> ToDictionary()
    {
        return _groups.ToDictionary(
            g => g.Key,
            g => (IDictionary<string, object?>)g.Value.Config
                .ToDictionary(c => c.Key, c => c.Value));
    }

    public void CopyTo(JobConfigGroup[] array, int arrayIndex)
    {
        _groups.Values.CopyTo(array, arrayIndex);
    }

    public IEnumerator<JobConfigGroup> GetEnumerator()
    {
        return _groups.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _groups.Values.GetEnumerator();
    }
}

public class JobConfigGroup
{
    public string Name { get; set; } = null!;

    public IDictionary<string, object?> Config { get; set; } = null!;
}
