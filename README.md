# 分时复用（TimeDivision）Ticker
该模块允许以更低的频率进行 tick，并且尽可能保证负载均衡。

例如，希望以进行 1/2 的频率进行 tick，那么将会产生 2 个“信道（`Channel`）”：

接下来向其中注册的函数（`action`）都会以每 2 帧执行一次的频率被调用，且默认情况下，连续的两次注册会把两个 `action` 放到两个不同的信道上，这样它们就不会挤到一帧内同时被调用，以达到负载均衡的目的。

如果提供了`hint`，那么 `hint` 将会被用来指导这个 `action` 希望被挂在哪个信道上，其算法是 `channelIndex = hint % channels`。
注册函数的返回值 `handle` 可以用来反注册，可以用来计算下一次注册的 `hint`，如果希望下次注册的函数与其同信道（即保证同一帧执行），那么可以传入 `hint: handle`，否则传入 `hint: handle + 1`（channels > 1 时）。

在实现方面，为了尽可能降低内存和 GC 开销，内部使用了数组形式的链表 + freelist 的数据结构，所有的 actions 被存放在一个数组中，freelist 和每个 Channel 各拥有一个 `head`，从它出发可以遍历 freelist 或该信道的所有 `action` 数组下标。但代价是注册与反注册时需要从 `head` 开始遍历到对应位置再插入或删除，复杂度是 `O(n)` 的。

主模块只有 `TimeDivisionTicker.cs`。`TickDriver.cs` 实现了一个辅助的、可以直接驱动主模块 tick 的 Unity 组件。
