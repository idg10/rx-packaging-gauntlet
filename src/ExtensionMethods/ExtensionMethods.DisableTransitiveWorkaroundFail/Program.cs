// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT License.
// See the LICENSE file in the project root for more information. 

using System.Reactive.Linq;

SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

IObservable<int> numbers = Observable.Range(1, 10);
IObservable<int> numbersViaSyncContext = numbers.ObserveOn(SynchronizationContext.Current!);
numbers.Subscribe(x => Console.WriteLine($"Number: {x}"));
