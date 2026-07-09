# Low-Hanging Fruit Feature Report (Expanded)

## Goal
Generate a larger, practical backlog of small-to-medium features that are cheap to implement and useful to package consumers.

## Scoring
- Impact: 1-5
- Effort: XS / S / M
- Priority score: `Impact / Effort weight` where XS=1, S=2, M=3

## Top 15 (Do First)
1. `ObservedTransformedValue`: expose `TransformedValue` getter. (Impact 5, Effort XS)
2. `StateMachine`: add `HasState`, `HasCurrentState`, `TryChangeState`. (5, S)
3. `PushdownAutomaton`: add `CanPop`, `HistoryCount`, `TryPop`, `ClearHistory`. (4, XS)
4. `ObservedThreshold`: add `IsBelowThreshold` + `Threshold` getters. (4, XS)
5. `RandomAccessQueue`: add `TryGetAt` and `TrySetAt`. (4, XS)
6. `RingBuffer`: add `InsertRange(IEnumerable<T>)`. (4, XS)
7. `RingBuffer`: add `ToArray()` and `ToArrayNewestFirst()`. (4, S)
8. `GLPlayerPrefs`: add default-value overloads for array getters. (4, S)
9. `GLPlayerPrefs`: add `HasArray(scope, key)`. (3, XS)
10. `GLPlayerPrefs`: add `DeleteScope(scope)`. (4, S)
11. `FixedKeyDictionary`: `Validate(idsInOrder, removeUnknownKeys)`. (4, S)
12. `ScreenshotTaker`: configurable output directory. (4, S)
13. `ExtensionsTools` editor menu: “Open Changelog”. (3, XS)
14. `ExtensionsTools` editor menu: “Open Package Folder”. (3, XS)
15. `ObservedValue`: add `SetSilently(T value, bool updatePrevious)` overload. (3, XS)

## Full Suggestion Backlog (35 items)

### Runtime Patterns
1. `ObservedTransformedValue` add `TransformedValue` property.
- File: `Runtime/Patterns/ObservedTransformedValue.cs`
- Why: avoids recomputing transform in callsites.

2. `ObservedTransformedValue` add `Refresh()` method.
- Recompute transformed value from current `Value` manually.

3. `ObservedTransformedValue` add optional comparer overload.
- Constructor overload with `IEqualityComparer<TTransformedValue>`.

4. `ObservedThreshold` expose read-only `Threshold`.

5. `ObservedThreshold` expose `IsBelowThreshold` state.

6. `ObservedThreshold` add `SetThreshold(float)`.
- Optional mutable threshold for tuning UIs.

7. `ObservedValue` add `ValueChanged(T oldValue, T newValue)` event.
- Keep existing `OnValueChange` for compatibility.

8. `StateMachine` add `TryChangeState(TLabel)`.

9. `StateMachine` add `HasState(TLabel)`.

10. `StateMachine` add `IReadOnlyCollection<TLabel> States`.

11. `StateMachine` add `OnStateChanged(TLabel from, TLabel to)` event.

12. `PushdownAutomaton` add `TryPop()`.

13. `PushdownAutomaton` add `HistoryCount` and `CanPop`.

14. `PushdownAutomaton` add `ClearHistory()`.

### Runtime Algorithms / Collections
15. `RandomAccessQueue` add `TryGetAt(int, out T)`.

16. `RandomAccessQueue` add `TrySetAt(int, T)`.

17. `RandomAccessQueue` add `EnqueueRange(IEnumerable<T>)`.

18. `RandomAccessQueue` add `DequeueMany(int count)` iterator.

19. `RandomAccessQueue` add `TrimExcess(float threshold)` overload.
- Caller-controlled threshold.

20. `RingBuffer` add `InsertRange(IEnumerable<T>)`.

21. `RingBuffer` add `ToArray()` in oldest->newest order.

22. `RingBuffer` add `ToArrayNewestFirst()`.

23. `RandomAccessPriorityQueue` add `TryRemove(int index)`.
- Returns false instead of throwing when missing.

24. `RandomAccessPriorityQueue` add `TryUpdateValue(...)`.

25. `CollectionExtensions` add `ForEachIndexed`.
- Very common utility in gameplay scripts.

26. `CollectionExtensions` add `IndexOfMaxBy` and `IndexOfMinBy`.

### Runtime Unity Extensions / Utilities
27. `GLPlayerPrefs` add default-array overloads for all array getters.

28. `GLPlayerPrefs` add `HasArray(scope, key)`.

29. `GLPlayerPrefs` add `DeleteScope(scope)`.

30. `GLPlayerPrefs` add `GetOrSet*` helpers (int/float/string/bool).
- E.g. `GetOrSetInt(scope,key,defaultValue)`.

31. `ScreenshotTaker` configurable output directory.

32. `ScreenshotTaker` add optional timestamp format string.

33. `ScreenshotTaker` add optional callback with saved path.
- `event Action<string> ScreenshotSaved`.

34. `TransformExtensions` add `SetLocalPosition(Vector3)` fluent helpers returning `Transform`.
- Useful for chaining.

35. `GameObjectExtensions` add `GetOrAddComponent<T>()`.
- High-frequency Unity helper.

### Inspectable Data
36. `FixedKeyDictionary` add `Validate(..., removeUnknownKeys)` overload.

37. `FixedKeyDictionary` add `ReorderKeys(IEnumerable<TKey>)` without add/remove.

38. `FixedKeyDictionary` add `TryGetIndex(TKey, out int)`.

### Editor / Tooling
39. `ExtensionsTools` menu action: open changelog.

40. `ExtensionsTools` menu action: open package root folder.

41. `ExtensionsTools` menu action: copy package version to clipboard.

42. Property drawer diagnostics toggle (verbose logs) via `PropertyDrawerData`.

43. Add “validate all serialized attributes in current scene” tool.

44. Add “validate all serialized attributes in open prefabs” tool.

### Docs / Quality-of-Life
45. Add quick-start snippets for top 10 classes to docs.

46. Add migration cheatsheet markdown for obsolete types.

47. Add XML docs examples for `RandomAccessQueue`, `RingBuffer`, `ObservedThreshold`.

48. Add API matrix page: “class vs. intended use” (pools, trackers, queues).

49. Add troubleshooting page for common null-reference pitfalls in Unity callbacks.

50. Add minimal test scaffolding docs (recommended assembly layout).

## Suggested Delivery Plan (2 sprints)

### Sprint 1 (all XS/S)
- Items: 1, 4, 5, 8, 9, 12, 13, 15, 20, 27, 28, 39, 40
- Outcome: broad API ergonomics uplift with near-zero migration cost.

### Sprint 2 (small but slightly broader behavior)
- Items: 6, 10, 11, 17, 21, 23, 29, 31, 35, 36, 41, 45
- Outcome: stronger runtime usability + better editor discoverability.

## Notes
- Keep all additions backward-compatible.
- Prefer additive overloads over behavior-changing edits.
- If uncertain, gate new behavior with explicit flags/overloads.
