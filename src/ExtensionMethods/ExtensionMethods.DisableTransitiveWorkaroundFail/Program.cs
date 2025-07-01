

using System.Reactive.Linq;

SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

IObservable<int> numbers = Observable.Range(1, 10);
IObservable<int> numbersViaSyncContext = numbers.ObserveOn(SynchronizationContext.Current!);
numbers.Subscribe(x => Console.WriteLine($"Number: {x}"));