# Yort.Trashy

## What is Yort.Trashy ?
Yort.Trashy is .Net library for implementing the disposable pattern correctly and in a thread-safe manner, as well was working with disposable types.

[![GitHub license](https://img.shields.io/github/license/mashape/apistatus.svg)](https://github.com/Yortw/Yort.Trashy/blob/master/LICENSE.md) 


## Supported Platforms
Currently;

* .Net Standard 1.3 (no stack trace capture available)
* Xamarin.iOS
* Xamarin.Android
* Net 4.0+

## Build Status
[![Build status](https://ci.appveyor.com/api/projects/status/8jw3sdsikltac4yr?svg=true)](https://ci.appveyor.com/project/Yortw/yort-trashy)

## What's in the box/How do I use it?

* Implement the dispose pattern correctly, including "thread-safety" and "idempotency".
    * Via [[inheritance|Implement-IDisposable-via-Inheritance]] (easiest).
    * Via [[composition|Implement-IDisposable-via-Composition]] (more work, allows other base classes).
* Implement the [[IIsDisposable]] interface for components that want to expose their disposed status.
* Dispose objects easily with [[TryDispose]], i.e ignore nulls, avoid manually casting to IDisposable, optionally suppress errors during disposal.
* Dispose collections of items with [[TryDispose]].
* Dispose multiple objects with [[DisposeAll]].
* Inherit from and use [[ReferenceCountedDisposableBase]] to prevent being disposed too early.
* [[Track and log disposable object instances]], including those that are not properly (explicitly) disposed.


Check out the [wiki](https://github.com/Yortw/Yort.Trashy/wiki) for more.
