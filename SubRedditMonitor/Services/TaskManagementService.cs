using System.Collections.Concurrent;

namespace SubRedditMonitor.Services;

public class TaskManagementService
{

    private const int PercentageOfMachineCpu = 60;// 60% of available CPUs
    private const int MaximumLengthOfTaskList = 100;

    private ConcurrentBag<Task> _allTasks = new ConcurrentBag<Task>();
    private readonly ConcurrentQueue<ITaskInfo> _taskQueue = new ConcurrentQueue<ITaskInfo>();
    private readonly SemaphoreSlim _semaphore;

    public event Action<int, int>? TasksRemainingChanged;

    public TaskManagementService()
    {
        var maxDegreeOfParallelism = Environment.ProcessorCount * MaxCpuPercentCount / 100; // 60% of available CPUs

        if (maxDegreeOfParallelism <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism), "Must be greater than zero.");

        _semaphore = new SemaphoreSlim(maxDegreeOfParallelism, maxDegreeOfParallelism);
    }
    /// <summary>
    /// How many tasks to run concurrently
    /// Default: 50 Tasks
    /// </summary>
    public int MaxTaskCountInRunMode { get; set; } = MaximumLengthOfTaskList;

    /// <summary>
    /// Percent of available CPU of the host to be used for running tasks
    /// Default is 60% ( if Host has 10 core CPU, 6 of them will be used.
    /// </summary>
    public int MaxCpuPercentCount { get; set; } = PercentageOfMachineCpu;

    /// <summary>
    /// Gets the number of tasks remaining in the queue.
    /// </summary>
    public int TasksWaiting => _taskQueue.Count;

    /// <summary>
    /// Gets the number of running tasks in the queue.
    /// </summary>
    public int TasksRunning => _allTasks.Count(t => !(t.IsCanceled || t.IsFaulted || t.IsCompletedSuccessfully || t.IsCanceled));

    /// <summary>
    /// Enqueues a task with a request and response format.
    /// </summary>
    /// <param name="TRequest">The type of the request object.</param>
    /// <param name="TResponse">The type of the response object.</param>
    /// <param name="request">The request object to be processed.</param>
    /// <param name="taskFunction">The asynchronous task function that processes the request and returns a TResponse.</param>
    /// <param name="callback">Optional callback to invoke with the response or an exception.</param>
    public void EnqueueTask<TRequest, TResponse>(TRequest request, Func<TRequest, Task<TResponse>> taskFunction, Action<TResponse?, Exception?>? callback = null)
    {
        if (taskFunction == null)
            throw new ArgumentNullException(nameof(taskFunction));

        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var taskInfo = new TaskInfo<TResponse>(
            async () => await taskFunction(request).ConfigureAwait(false),
            (response, exception) =>
            {
                callback?.Invoke(response, exception);
            });

        _taskQueue.Enqueue(taskInfo);
        _allTasks.Add(taskInfo.Task);

        TasksRemainingChanged?.Invoke(_taskQueue.Count, _allTasks.Count);

        // Start processing the task queue
        _ = ProcessQueueAsync();
    }



    /// <summary>
    /// Enqueues a task that returns a result with an optional callback.
    /// </summary>
    /// <param name="T">The type of the result.</param>
    /// <param name="taskFunction">The asynchronous task function to execute.</param>
    /// <param name="callback">Optional callback to invoke upon task completion or fault.</param>
    public void EnqueueTask<T>(Func<Task<T>> taskFunction, Action<T, Exception>? callback = null)
    {
        if (taskFunction == null)
            throw new ArgumentNullException(nameof(taskFunction));

        var taskInfo = new TaskInfo<T>(taskFunction, callback!);
        _taskQueue.Enqueue(taskInfo);
        _allTasks.Add(taskInfo.Task);

        TasksRemainingChanged?.Invoke(_taskQueue.Count, _allTasks.Count);

        _ = ProcessQueueAsync();
    }

    /// <summary>
    /// Enqueues a task that does not return a result with an optional callback.
    /// </summary>
    /// <param name="taskFunc">The asynchronous task function to execute.</param>
    /// <param name="callback">Optional callback to invoke upon task completion or fault.</param>
    public void EnqueueTask(Func<Task> taskFunc, Action<Exception?>? callback = null)
    {
        if (taskFunc == null)
            throw new ArgumentNullException(nameof(taskFunc));

        var taskInfo = new TaskInfoVoid(taskFunc, callback);
        _taskQueue.Enqueue(taskInfo);
        _allTasks.Add(taskInfo.Task);

        TasksRemainingChanged?.Invoke(_taskQueue.Count, _allTasks.Count);

        // Start processing tasks if possible
        _ = ProcessQueueAsync();
    }


    /// <summary>
    /// Asynchronously waits for all queued and running tasks to complete.
    ///
    /// IMPORTANT: We can improve this method to call an optional call back to removed completed tasks.
    ///            This will help to keep the memory usage low.
    /// </summary>
    /// <returns>A Task representing the asynchronous wait operation.</returns>
    public async Task WaitAllTasksAsync()
    {
        Task[] tasksToWait;

        // capture a snapshot of all tasks
        lock (_allTasks)
        {
            tasksToWait = _allTasks.ToArray();
        }

        await Task.WhenAll(tasksToWait).ConfigureAwait(false);
    }

    /// <summary>
    /// Clears all queued tasks. Note that running tasks will continue to completion.
    /// </summary>
    public void ClearQueuedTasks()
    {
        while (_taskQueue.TryDequeue(out _)) { }

        TasksRemainingChanged?.Invoke(_taskQueue.Count, _allTasks.Count);
    }


    /// <summary>
    /// Asynchronously processes tasks in the queue.
    /// </summary>
    private async Task ProcessQueueAsync()
    {

        CleanTaskListIfMaxReached();

        while (true)
        {
            if (!_taskQueue.TryDequeue(out ITaskInfo? taskInfo))
            {
                break; // No more tasks to process
            }

            TasksRemainingChanged?.Invoke(_taskQueue.Count, _allTasks.Count);

            await _semaphore.WaitAsync().ConfigureAwait(false);
            var executionTask = ExecuteTaskAsync(taskInfo);

            // Detach the task to allow it to run independently
            _ = executionTask;
        }
    }

    /// <summary>
    /// Executes a single task and handles semaphore release.
    /// </summary>
    /// <param name="taskInfo">The task information to execute.</param>
    private async Task ExecuteTaskAsync(ITaskInfo? taskInfo)
    {
        try
        {
            if (taskInfo != null)
                await taskInfo.ExecuteAsync().ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// returns a new ConcurrentBag with active and running tasks.
    /// </summary>
    private ConcurrentBag<Task> CleanUpTasksBag()
    {
        var newTasks = new ConcurrentBag<Task>();

        foreach (var task in _allTasks)
        {
            if (!task.IsCompleted || task.IsFaulted || task.IsCanceled)
            {
                newTasks.Add(task);
            }
        }

        // Return the new ConcurrentBag with only the non-completed/non-faulted tasks
        return newTasks;
    }

    private void CleanTaskListIfMaxReached()
    {
        if (_allTasks.Count >= MaxTaskCountInRunMode)
            _allTasks = CleanUpTasksBag();
    }

    private interface ITaskInfo
    {
        Task ExecuteAsync();
    }


    /// <summary>
    /// Generic class to hold task information for tasks with return values
    /// </summary>
    /// <param name="T"></param>
    private class TaskInfo<T>(Func<Task<T>> taskFunc, Action<T?, Exception?>? callback) : ITaskInfo
    {
        private readonly TaskCompletionSource<T?> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<T?> Task => _tcs.Task;

        public async Task ExecuteAsync()
        {
            try
            {
                var result = await taskFunc().ConfigureAwait(false);
                _tcs.SetResult(result);
                callback?.Invoke(result, null);
            }
            catch (Exception? ex)
            {
                _tcs.SetException(ex);
                callback?.Invoke(default, ex);
            }
        }
    }

    /// <summary>
    /// Class to hold task information for tasks without return values
    /// </summary>
    private class TaskInfoVoid(Func<Task> taskFunc, Action<Exception?>? callback) : ITaskInfo
    {
        private readonly TaskCompletionSource<object?> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Task => _tcs.Task;

        public async Task ExecuteAsync()
        {
            try
            {
                await taskFunc().ConfigureAwait(false);

                _tcs.SetResult(null);
                callback?.Invoke(null);
            }
            catch (Exception ex)
            {
                _tcs.SetException(ex);
                callback?.Invoke(ex);
            }
        }
    }

    /// <summary>
    /// Generic class to hold task information for tasks with return values,
    /// allowing the result to be transformed into a different type.
    /// </summary>
    /// <param name="T">The type of the task result.</param>
    /// <param name="TResult">The type of the transformed result returned by the ExecuteAsync method.</param>
    private class TaskInfo<T, TResult> : ITaskInfo
    {
        private readonly TaskCompletionSource<TResult> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Func<Task<T>> _taskFunc;
        private readonly Func<T, TResult> _transform; // Function to transform the task result
        private readonly Action<TResult?, Exception?>? _callback;

        public Task<TResult> Task => _tcs.Task;

        public TaskInfo(Func<Task<T>> taskFunc, Func<T, TResult> transform, Action<TResult?, Exception?>? callback)
        {
            _taskFunc = taskFunc;
            _transform = transform;
            _callback = callback;
        }

        public async Task ExecuteAsync()
        {
            try
            {
                var taskResult = await _taskFunc().ConfigureAwait(false);
                var transformedResult = _transform(taskResult);

                _tcs.SetResult(transformedResult);
                _callback?.Invoke(transformedResult, null);
            }
            catch (Exception? ex)
            {
                _tcs.SetException(ex);
                _callback?.Invoke(default, ex);
            }
        }
    }

}
