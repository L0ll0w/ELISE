# Problems

- `Cache.cs`: Six members of an inner class throw `NotImplementedException` with no indication that this is intentional — `Count`, `IsFull`, `ContainsKey`, `Remove`, `RemoveOldest`, and the indexer. Looks like an incomplete implementation rather than a designed extension point.
