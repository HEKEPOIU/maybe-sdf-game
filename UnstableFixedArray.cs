using System;
using System.Collections;
using System.Collections.Generic;

namespace GodotTool;

public class UnstableFixedArray<T>(int capacity) : IEnumerable<T>
{
	private readonly T[] array = new T[capacity];
	public int Count { get; private set; } = 0;
	public int Length => Count;

	public void Add(T item)
	{
		if (Count >= array.Length) throw new IndexOutOfRangeException();
		array[Count] = item;
		Count++;
	}

	public void RemoveAll<Child>() where Child : T
	{
		for (int i = Count - 1; i >= 0; i--)
		{
			if (array[i] is Child)
				RemoveByIndex(i);
		}
	}

	public void Remove(T item)
	{
		RemoveByIndex(IndexOf(item));
	}

	public void RemoveByIndex(int i)
	{
		(array[i], array[Count-1]) = (array[Count-1], array[i]);
		Count--;
	}

	public void Clear()
	{
		Count = 0;
	}

	public IEnumerator<T> GetEnumerator()
	{
		for (int i = 0; i < Count; i++)
			yield return array[i];

	}
	public bool Contains(T item) => Array.IndexOf(array, item, 0, Count) >= 0;
	public bool Contains<Child>() where Child : T
	{
		for (int i = 0; i < Count; i++)
			if (array[i] is Child)
				return true;
		return false;
	}
	public int IndexOf(T item) => Array.IndexOf(array, item, 0, Count);

	public Child Find<Child>() where Child : T
	{
		for (int i = 0; i < Count; i++)
			if (array[i] is Child child)
				return child;
		return default;
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	public T this[int index]
	{
		get
		{
			if (index < 0 || index >= Count) throw new IndexOutOfRangeException();
			return array[index];
		}
		set
		{
			if (index < 0 || index >= Count) throw new IndexOutOfRangeException();
			array[index] = value;
		}
	}
}
