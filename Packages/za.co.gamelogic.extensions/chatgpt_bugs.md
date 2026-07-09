Problem: Runtime/Algorithms/RandomAccessQueue.cs:93-94 initializes tail before Count
Description of issue: In the IEnumerable constructor, `tail = Count` executes before `Count = items.Length`, so `tail` is always 0. The first Enqueue after constructing from a non-empty collection overwrites the first element and corrupts queue ordering.
Snippet with fix:
```csharp
public RandomAccessQueue(IEnumerable<T> collection)
{
    items = collection.ToArray();
    head = 0;
    Count = items.Length;
    tail = Count;
    version = 0;
}
```
------
Problem: Runtime/Algorithms/RandomAccessQueue.cs:117 can throw NullReferenceException in Contains
Description of issue: `ElementAtUnchecked(i).Equals(element)` dereferences queue elements directly. If stored element is null (valid for reference types), Contains throws instead of returning true/false.
Snippet with fix:
```csharp
public bool Contains(T element)
{
    for (int i = 0; i < Count; i++)
    {
        if (EqualityComparer<T>.Default.Equals(ElementAtUnchecked(i), element))
        {
            return true;
        }
    }

    return false;
}
```
------
Problem: Runtime/Patterns/Pool.cs:143-166 corrupts ActiveCount when shrinking capacity
Description of issue: `DecreaseCapacity` can destroy active objects but never decrements `ActiveCount`. This leaves `ActiveCount > Capacity`, causing index errors and invalid pool state on next `Get`/`Release`.
Snippet with fix:
```csharp
public int DecreaseCapacity(int decrement, bool deactivateFirst = false)
{
    decrement.ThrowIfNegative(nameof(decrement));

    int initialCapacity = Capacity;
    int remainingObjectsCount = Mathf.Max(0, Capacity - decrement);
    int destroyCount = Capacity - remainingObjectsCount;

    for (int i = remainingObjectsCount; i < initialCapacity; i++)
    {
        var obj = poolObjects[i];

        if (i < ActiveCount)
        {
            if (deactivateFirst)
            {
                deactivate?.Invoke(obj);
            }

            ActiveCount--;
        }

        destroy?.Invoke(obj);
    }

    poolObjects.RemoveRange(remainingObjectsCount, destroyCount);
    return destroyCount;
}
```
------
Problem: Runtime/Patterns/Pool.cs:161,181,225 invoke optional callbacks without null checks
Description of issue: `destroy` and `deactivate` are documented/typed as optional, but several call sites invoke them directly. This throws NullReferenceException when callbacks are intentionally omitted.
Snippet with fix:
```csharp
// Pool.DecreaseCapacity
if (i < ActiveCount && deactivateFirst)
{
    deactivate?.Invoke(obj);
}
destroy?.Invoke(obj);

// Pool.ReleaseAll
deactivate?.Invoke(poolObjects[i]);

// Pool.DeactivateAndReorderObjectAt
deactivate?.Invoke(obj);
```
------
Problem: Runtime/Patterns/HashPool.cs:148 invokes optional deactivate callback directly
Description of issue: `HashPool` accepts nullable `deactivate`, but `ReleaseAll` calls `deactivate(obj)` without null-conditional. Calling ReleaseAll with no deactivate callback throws NullReferenceException.
Snippet with fix:
```csharp
while (enumerator.MoveNext())
{
    var obj = enumerator.Current;
    deactivate?.Invoke(obj);
    inactiveObjects.Push(obj);
}
```
------
Problem: Runtime/UnityExtensions/TransformExtensions.cs:195 uses Z value as local Y in SetLocalXZ
Description of issue: `SetLocalXZ` builds `(x, transform.localPosition.z, z)` instead of preserving Y. Calling it silently changes Y to previous Z.
Snippet with fix:
```csharp
public static void SetLocalXZ(this Transform transform, float x, float z)
{
    var newPosition = new Vector3(x, transform.localPosition.y, z);
    transform.localPosition = newPosition;
}
```
------
Problem: Runtime/UnityExtensions/TransformExtensions.cs:392-396 does not scale in ScaleByXYZ
Description of issue: `ScaleByXYZ` assigns absolute scale `(x, y, z)` but method name/summary promise multiplicative scaling.
Snippet with fix:
```csharp
public static void ScaleByXYZ(this Transform transform, float x, float y, float z)
{
    transform.localScale = new Vector3(
        transform.localScale.x * x,
        transform.localScale.y * y,
        transform.localScale.z * z);
}
```
------
Problem: Runtime/UnityExtensions/TransformExtensions.cs:481 flips X using Z in FlipXYZ
Description of issue: `FlipXYZ` passes `-transform.localScale.z` as first argument, so X is set from Z instead of from X.
Snippet with fix:
```csharp
public static void FlipXYZ(this Transform transform)
{
    transform.SetScaleXYZ(
        -transform.localScale.x,
        -transform.localScale.y,
        -transform.localScale.z);
}
```
------
Problem: Runtime/Utilities/ScreenshotTaker.cs:45/177 null default for screenshotOnlyObjects causes foreach crash
Description of issue: `screenshotOnlyObjects` defaults to null, but `SetScreenshotOnlyObjects` always iterates it. Calling TakeClean/TakeTexture without assigning this field throws NullReferenceException.
Snippet with fix:
```csharp
[SerializeField]
private GameObject[] screenshotOnlyObjects = Array.Empty<GameObject>();
```
------
Problem: Runtime/Algorithms/Buffer/RingBuffer.cs:49 with capacity 0 causes dequeue from empty on first insert
Description of issue: Constructor allows zero capacity. On first Insert, `queue.Count == Capacity` is true (0 == 0), then `queue.Dequeue()` is called on an empty queue and throws.
Snippet with fix:
```csharp
public RingBuffer(int capacity)
{
    if (capacity <= 0)
    {
        throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than 0.");
    }

    Capacity = capacity;
}
```
------
