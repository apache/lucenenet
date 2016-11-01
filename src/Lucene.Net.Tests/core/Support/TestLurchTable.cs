using Lucene.Net.Attributes;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Support
{
    #region Copyright 2012-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
    /* Licensed under the Apache License, Version 2.0 (the "License");
     * you may not use this file except in compliance with the License.
     * You may obtain a copy of the License at
     * 
     *   http://www.apache.org/licenses/LICENSE-2.0
     * 
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */
    #endregion

    [TestFixture]
    public class TestLurchTable : TestGenericCollection<TestLurchTable.LurchTableTest<int, string>, KeyValuePair<int, string>>
    {
        public class LurchTableTest<TKey, TValue> : LurchTable<TKey, TValue>
        {
            public LurchTableTest() : base(1024, LurchTableOrder.Access)
            { }
            public LurchTableTest(LurchTableOrder order) : base(1024, order)
            { }
            public LurchTableTest(IEqualityComparer<TKey> comparer) : base(1024, LurchTableOrder.Access, comparer)
            { }
        }

        protected override KeyValuePair<int, string>[] GetSample()
        {
            var results = new List<KeyValuePair<int, string>>();
            Random r = new Random();
            for (int i = 1; i < 100; i += r.Next(1, 3))
                results.Add(new KeyValuePair<int, string>(i, i.ToString()));
            return results.ToArray();
        }

        class IntComparer : IEqualityComparer<int>
        {
            bool IEqualityComparer<int>.Equals(int x, int y)
            {
                return false;
            }

            int IEqualityComparer<int>.GetHashCode(int obj)
            {
                return 0;
            }
        }
        [Test, LuceneNetSpecific]
        public void TestCTors()
        {
            var cmp = new IntComparer();
            const int limit = 5;

            Assert.AreEqual(LurchTableOrder.None, new LurchTable<int, int>(1).Ordering);
            Assert.AreEqual(LurchTableOrder.Insertion, new LurchTable<int, int>(1, LurchTableOrder.Insertion).Ordering);
            Assert.IsTrue(ReferenceEquals(cmp, new LurchTable<int, int>(1, LurchTableOrder.Insertion, cmp).Comparer));
            Assert.AreEqual(LurchTableOrder.Modified, new LurchTable<int, int>(LurchTableOrder.Modified, limit).Ordering);
            Assert.AreEqual(limit, new LurchTable<int, int>(LurchTableOrder.Modified, limit).Limit);
            Assert.AreEqual(LurchTableOrder.Access, new LurchTable<int, int>(LurchTableOrder.Access, limit, cmp).Ordering);
            Assert.AreEqual(limit, new LurchTable<int, int>(LurchTableOrder.Access, limit, cmp).Limit);
            Assert.IsTrue(ReferenceEquals(cmp, new LurchTable<int, int>(LurchTableOrder.Access, limit, cmp).Comparer));
            Assert.AreEqual(LurchTableOrder.Access, new LurchTable<int, int>(LurchTableOrder.Access, limit, 1, 1, 1, cmp).Ordering);
            Assert.AreEqual(limit, new LurchTable<int, int>(LurchTableOrder.Access, limit, 1, 1, 1, cmp).Limit);
            Assert.IsTrue(ReferenceEquals(cmp, new LurchTable<int, int>(LurchTableOrder.Access, limit, 1, 1, 1, cmp).Comparer));
        }

        [Test, LuceneNetSpecific]
        public void TestDequeueByInsertion()
        {
            var test = new LurchTableTest<int, string>(LurchTableOrder.Insertion);
            Assert.AreEqual(LurchTableOrder.Insertion, test.Ordering);
            var sample = GetSample();
            Array.Reverse(sample);
            foreach (var item in sample)
                test.Add(item.Key, item.Value);

            KeyValuePair<int, string> value;
            foreach (var item in sample)
            {
                Assert.IsTrue(test.Peek(out value));
                Assert.AreEqual(item.Key, value.Key);
                Assert.AreEqual(item.Value, value.Value);
                value = test.Dequeue();
                Assert.AreEqual(item.Key, value.Key);
                Assert.AreEqual(item.Value, value.Value);
            }

            Assert.IsFalse(test.Peek(out value));
            Assert.IsFalse(test.TryDequeue(out value));
        }

        [Test, LuceneNetSpecific]
        public void TestDequeueByModified()
        {
            var test = new LurchTableTest<int, string>(LurchTableOrder.Modified);
            Assert.AreEqual(LurchTableOrder.Modified, test.Ordering);
            var sample = GetSample();
            foreach (var item in sample)
                test.Add(item.Key, item.Value);

            Array.Reverse(sample);
            for (int i = 0; i < sample.Length; i++)
            {
                var item = new KeyValuePair<int, string>(sample[i].Key, sample[i].Value + "-2");
                test[item.Key] = item.Value;
                sample[i] = item;
            }

            KeyValuePair<int, string> value;
            foreach (var item in sample)
            {
                Assert.IsTrue(test.Peek(out value));
                Assert.AreEqual(item.Key, value.Key);
                Assert.AreEqual(item.Value, value.Value);
                value = test.Dequeue();
                Assert.AreEqual(item.Key, value.Key);
                Assert.AreEqual(item.Value, value.Value);
            }

            Assert.IsFalse(test.Peek(out value));
            Assert.IsFalse(test.TryDequeue(out value));
        }

        [Test, LuceneNetSpecific]
        public void TestDequeueByAccess()
        {
            var test = new LurchTableTest<int, string>(LurchTableOrder.Access);
            Assert.AreEqual(LurchTableOrder.Access, test.Ordering);
            var sample = GetSample();
            foreach (var item in sample)
                test.Add(item.Key, item.Value);

            Array.Reverse(sample);
            foreach (var item in sample)
                Assert.AreEqual(item.Value, test[item.Key]);

            KeyValuePair<int, string> value;
            foreach (var item in sample)
            {
                Assert.IsTrue(test.TryDequeue(out value));
                Assert.AreEqual(item.Key, value.Key);
                Assert.AreEqual(item.Value, value.Value);
            }

            Assert.IsFalse(test.Peek(out value));
            Assert.IsFalse(test.TryDequeue(out value));
        }

        [Test, LuceneNetSpecific]
        public void TestKeysEnumerator()
        {
            var sample = GetSample();
            var test = CreateSample(sample);
            int ix = 0;
            foreach (var key in test.Keys)
                Assert.AreEqual(sample[ix++].Value, test[key]);
        }

        [Test, LuceneNetSpecific]
        public void TestValuesEnumerator()
        {
            var sample = GetSample();
            var test = CreateSample(sample);
            int ix = 0;
            foreach (var value in test.Values)
                Assert.AreEqual(sample[ix++].Value, value);
        }

        [Test, LuceneNetSpecific]
        public void TestLimitorByAccess()
        {
            //multiple of prime will produce hash collision, thus testing removal of non-first elements
            const int prime = 1103;
            var test = new LurchTable<int, string>(LurchTableOrder.Access, 3, prime, 10, 10, EqualityComparer<int>.Default);
            test[1 * prime] = "a";
            test[2 * prime] = "b";
            test[3 * prime] = "c";
            Assert.AreEqual(3, test.Count);
            Assert.AreEqual("b", test[2 * prime]); //access moves to front..
            test[4] = "d";
            test[5] = "e";
            Assert.AreEqual(3, test.Count); // still 3 items
            Assert.IsFalse(test.ContainsKey(1 * prime));
            Assert.IsTrue(test.ContainsKey(2 * prime)); //recently access is still there
            Assert.IsFalse(test.ContainsKey(3 * prime));
        }

        class RecordEvents<TKey, TValue>
        {
            public KeyValuePair<TKey, TValue> LastAdded, LastUpdate, LastRemove;

            public void ItemAdded(KeyValuePair<TKey, TValue> obj) { LastAdded = obj; }
            public void ItemUpdated(KeyValuePair<TKey, TValue> original, KeyValuePair<TKey, TValue> obj) { LastUpdate = obj; }
            public void ItemRemoved(KeyValuePair<TKey, TValue> obj) { LastRemove = obj; }
        }

        [Test, LuceneNetSpecific]
        public void TestCrudEvents()
        {
            var recorder = new RecordEvents<int, string>();
            var test = new LurchTable<int, string>(LurchTableOrder.Access, 3, 1103, 10, 10, EqualityComparer<int>.Default);
            test.ItemAdded += recorder.ItemAdded;
            test.ItemUpdated += recorder.ItemUpdated;
            test.ItemRemoved += recorder.ItemRemoved;
            test[1] = "a";
            Assert.AreEqual("a", recorder.LastAdded.Value);
            test[2] = "b";
            Assert.AreEqual("b", recorder.LastAdded.Value);
            test[3] = "c";
            Assert.AreEqual("c", recorder.LastAdded.Value);
            Assert.AreEqual(3, test.Count);
            Assert.AreEqual("b", test[2]); //access moves to front..
            test[4] = "d";
            Assert.AreEqual("d", recorder.LastAdded.Value);
            Assert.AreEqual("a", recorder.LastRemove.Value);
            test[5] = "e";
            Assert.AreEqual("e", recorder.LastAdded.Value);
            Assert.AreEqual("c", recorder.LastRemove.Value);
            test[2] = "B";
            Assert.AreEqual("B", recorder.LastUpdate.Value);
            test[6] = "f";
            Assert.AreEqual("f", recorder.LastAdded.Value);
            Assert.AreEqual("d", recorder.LastRemove.Value);

            Assert.AreEqual(3, test.Count); // still 3 items
            string value;
            Assert.IsTrue(test.TryRemove(5, out value));
            Assert.AreEqual("e", value);
            Assert.AreEqual("e", recorder.LastRemove.Value);

            Assert.AreEqual("B", test.Dequeue().Value);
            Assert.AreEqual("f", test.Dequeue().Value);
            Assert.AreEqual(0, test.Count);
        }

        [Test, LuceneNetSpecific]
        public void TestCollisionRemoval()
        {
            //multiple of prime will produce hash collision, thus testing removal of non-first elements
            const int prime = 1103;
            var test = new LurchTable<int, string>(LurchTableOrder.Access, 10, prime, 10, 10, EqualityComparer<int>.Default);
            test[1 * prime] = "a";
            test[2 * prime] = "b";
            test[3 * prime] = "c";
            test[4 * prime] = "d";
            test[5 * prime] = "e";
            Assert.IsTrue(test.Remove(4 * prime));
            Assert.IsTrue(test.Remove(2 * prime));
            Assert.IsTrue(test.Remove(5 * prime));
            Assert.IsTrue(test.Remove(1 * prime));
            Assert.IsTrue(test.Remove(3 * prime));
            Assert.AreEqual(0, test.Count);
        }

        [Test, LuceneNetSpecific]
        public void TestAddRemoveByKey()
        {
            LurchTableTest<int, string> test = new LurchTableTest<int, string>();
            for (int i = 0; i < 10; i++)
                test.Add(i, i.ToString());

            for (int i = 0; i < 10; i++)
                Assert.IsTrue(test.ContainsKey(i));

            string cmp;
            for (int i = 0; i < 10; i++)
                Assert.IsTrue(test.TryGetValue(i, out cmp) && cmp == i.ToString());

            for (int i = 0; i < 10; i++)
                Assert.IsTrue(test.Remove(i));
        }

        [Test, LuceneNetSpecific]
        public void TestComparer()
        {
            var test = new LurchTableTest<string, string>(StringComparer.OrdinalIgnoreCase);
            test["a"] = "b";
            Assert.IsTrue(test.ContainsKey("A"));

            test = new LurchTableTest<string, string>(StringComparer.OrdinalIgnoreCase);
            test["a"] = "b";
            Assert.IsTrue(test.ContainsKey("A"));
        }

        [Test, LuceneNetSpecific]
        public void TestKeys()
        {
            LurchTableTest<string, string> test = new LurchTableTest<string, string>();
            test["a"] = "b";
            string all = String.Join("", new List<string>(test.Keys).ToArray());
            Assert.AreEqual("a", all);
        }

        [Test, LuceneNetSpecific]
        public void TestValues()
        {
            LurchTableTest<string, string> test = new LurchTableTest<string, string>();
            test["a"] = "b";
            string all = String.Join("", new List<string>(test.Values).ToArray());
            Assert.AreEqual("b", all);
        }

        [Test, LuceneNetSpecific]
        public void TestAtomicAdd()
        {
            var data = new LurchTableTest<int, string>();
            int[] counter = new int[] { -1 };
            for (int i = 0; i < 100; i++)
                Assert.IsTrue(data.TryAdd(i, (k) => (++counter[0]).ToString()));
            Assert.AreEqual(100, data.Count);
            Assert.AreEqual(100, counter[0] + 1);

            //Inserts of existing keys will not call method
            Assert.IsFalse(data.TryAdd(50, (k) => { throw new InvalidOperationException(); }));
            Assert.AreEqual(100, data.Count);
        }

        [Test, LuceneNetSpecific]
        public void TestAtomicAddOrUpdate()
        {
            var data = new LurchTableTest<int, string>();
            int[] counter = new int[] { -1 };

            for (int i = 0; i < 100; i++)
                data.AddOrUpdate(i, (k) => (++counter[0]).ToString(), (k, v) => { throw new InvalidOperationException(); });

            for (int i = 0; i < 100; i++)
                Assert.AreEqual((i & 1) == 1, data.TryRemove(i, (k, v) => (int.Parse(v) & 1) == 1));

            for (int i = 0; i < 100; i++)
                data.AddOrUpdate(i, (k) => (++counter[0]).ToString(), (k, v) => (++counter[0]).ToString());

            Assert.AreEqual(100, data.Count);
            Assert.AreEqual(200, counter[0] + 1);

            for (int i = 0; i < 100; i++)
                Assert.IsTrue(data.TryRemove(i, (k, v) => int.Parse(v) - 100 == i));

            Assert.AreEqual(0, data.Count);
        }

        [Test, LuceneNetSpecific]
        public void TestNewAddOrUpdate()
        {
            var data = new LurchTableTest<int, string>();
            Assert.AreEqual("a", data.AddOrUpdate(1, "a", (k, v) => k.ToString()));
            Assert.AreEqual("1", data.AddOrUpdate(1, "a", (k, v) => k.ToString()));

            Assert.AreEqual("b", data.AddOrUpdate(2, k => "b", (k, v) => k.ToString()));
            Assert.AreEqual("2", data.AddOrUpdate(2, k => "b", (k, v) => k.ToString()));
        }

        struct AddUpdateValue : ICreateOrUpdateValue<int, string>, IRemoveValue<int, string>
        {
            public string OldValue;
            public string Value;
            public bool CreateValue(int key, out string value)
            {
                OldValue = null;
                value = Value;
                return Value != null;
            }
            public bool UpdateValue(int key, ref string value)
            {
                OldValue = value;
                value = Value;
                return Value != null;
            }
            public bool RemoveValue(int key, string value)
            {
                OldValue = value;
                return value == Value;
            }
        }

        [Test, LuceneNetSpecific]
        public void TestAtomicInterfaces()
        {
            var data = new LurchTableTest<int, string>();

            data[1] = "a";

            AddUpdateValue update = new AddUpdateValue();
            Assert.IsFalse(data.AddOrUpdate(1, ref update));
            Assert.AreEqual("a", update.OldValue);
            Assert.IsFalse(data.AddOrUpdate(2, ref update));
            Assert.IsNull(update.OldValue);
            Assert.IsFalse(data.TryRemove(1, ref update));
            Assert.AreEqual("a", update.OldValue);

            Assert.AreEqual(1, data.Count);
            Assert.AreEqual("a", data[1]);

            update.Value = "b";
            Assert.IsTrue(data.AddOrUpdate(1, ref update));
            Assert.AreEqual("a", update.OldValue);
            Assert.IsTrue(data.AddOrUpdate(2, ref update));
            Assert.IsNull(update.OldValue);

            Assert.AreEqual(2, data.Count);
            Assert.AreEqual("b", data[1]);
            Assert.AreEqual("b", data[2]);

            Assert.IsTrue(data.TryRemove(1, ref update));
            Assert.AreEqual("b", update.OldValue);
            Assert.IsTrue(data.TryRemove(2, ref update));
            Assert.AreEqual("b", update.OldValue);
            Assert.AreEqual(0, data.Count);
        }

        [Test, LuceneNetSpecific]
        public void TestGetOrAdd()
        {
            var data = new LurchTableTest<int, string>();
            Assert.AreEqual("a", data.GetOrAdd(1, "a"));
            Assert.AreEqual("a", data.GetOrAdd(1, "b"));

            Assert.AreEqual("b", data.GetOrAdd(2, k => "b"));
            Assert.AreEqual("b", data.GetOrAdd(2, k => "c"));
        }


        [Test, LuceneNetSpecific]
        public void TestTryRoutines()
        {
            var data = new LurchTableTest<int, string>();

            Assert.IsTrue(data.TryAdd(1, "a"));
            Assert.IsFalse(data.TryAdd(1, "a"));

            Assert.IsTrue(data.TryUpdate(1, "a"));
            Assert.IsTrue(data.TryUpdate(1, "c"));
            Assert.IsTrue(data.TryUpdate(1, "d", "c"));
            Assert.IsFalse(data.TryUpdate(1, "f", "c"));
            Assert.AreEqual("d", data[1]);
            Assert.IsTrue(data.TryUpdate(1, "a", data[1]));
            Assert.AreEqual("a", data[1]);
            Assert.IsFalse(data.TryUpdate(2, "b"));

            string val;
            Assert.IsTrue(data.TryRemove(1, out val) && val == "a");
            Assert.IsFalse(data.TryRemove(2, out val));
            Assert.AreNotEqual(val, "a");

            Assert.IsFalse(data.TryUpdate(1, (k, x) => x.ToUpper()));
            data[1] = "a";
            data[1] = "b";
            Assert.IsTrue(data.TryUpdate(1, (k, x) => x.ToUpper()));
            Assert.AreEqual("B", data[1]);
        }

        [Test, LuceneNetSpecific]
        public void TestInitialize()
        {
            LurchTableTest<string, string> test = new LurchTableTest<string, string>(StringComparer.Ordinal);
            test["a"] = "b";
            Assert.AreEqual(1, test.Count);
            test.Initialize();
            Assert.AreEqual(0, test.Count);
        }

        [Test, LuceneNetSpecific]
        public void TestSampleCollection()
        {
            var sample = GetSample();
            var items = CreateSample(sample);
            IDictionary<int, string> dict = items;
            VerifyCollection(new KeyValueEquality<int, string>(), new List<KeyValuePair<int, string>>(sample), items);
            VerifyCollection(new KeyValueEquality<int, string>(), new List<KeyValuePair<int, string>>(sample), dict);
        }

        [Test, LuceneNetSpecific]
        public void TestSampleKeyCollection()
        {
            var sample = GetSample();
            var items = CreateSample(sample);
            IDictionary<int, string> dict = items;
            var keys = new List<int>();
            foreach (var kv in sample)
                keys.Add(kv.Key);
            VerifyCollection(EqualityComparer<int>.Default, keys.AsReadOnly(), items.Keys);
            VerifyCollection(EqualityComparer<int>.Default, keys.AsReadOnly(), dict.Keys);
        }

        [Test, LuceneNetSpecific]
        public void TestSampleValueCollection()
        {
            var sample = GetSample();
            var items = CreateSample(sample);
            IDictionary<int, string> dict = items;
            var values = new List<string>();
            foreach (var kv in sample)
                values.Add(kv.Value);
            VerifyCollection(EqualityComparer<string>.Default, values.AsReadOnly(), items.Values);
            VerifyCollection(EqualityComparer<string>.Default, values.AsReadOnly(), dict.Values);
        }

        [Test, ExpectedException(typeof(ObjectDisposedException))]
        public void TestDisposed()
        {
            IDictionary<int, string> test = new LurchTableTest<int, string>();
            var disposable = test as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
            test.Add(1, "");
        }

        class KeyValueEquality<TKey, TValue> : IEqualityComparer<KeyValuePair<TKey, TValue>>
        {
            IEqualityComparer<TKey> KeyComparer = EqualityComparer<TKey>.Default;
            IEqualityComparer<TValue> ValueComparer = EqualityComparer<TValue>.Default;
            public bool Equals(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
            {
                return KeyComparer.Equals(x.Key, y.Key) && ValueComparer.Equals(x.Value, y.Value);
            }
            public int GetHashCode(KeyValuePair<TKey, TValue> obj)
            {
                return KeyComparer.GetHashCode(obj.Key) ^ ValueComparer.GetHashCode(obj.Value);
            }
        }


        #region TestGenericCollection<TList, TItem>
        // LUCENENET NOTE: Tests in an abstract base class are not pulled into the correct
        // context in Visual Studio. This fixes that with the minimum amount of code necessary
        // to run them in the correct context without duplicating all of the tests.

        [Test, LuceneNetSpecific]
        public void TestGenericCollection_TestAddRemove()
        {
            base.TestAddRemove();
        }

        [Test, LuceneNetSpecific]
        public void TestGenericCollection_TestAddReverseRemove()
        {
            base.TestAddReverseRemove();
        }

        [Test, LuceneNetSpecific]
        public void TestGenericCollection_TestClear()
        {
            base.TestClear();
        }

        [Test, LuceneNetSpecific]
        public void TestGenericCollection_TestContains()
        {
            base.TestContains();
        }

        [Test, LuceneNetSpecific]
        public void TestGenericCollection_TestCopyTo()
        {
            base.TestCopyTo();
        }

        [Test, LuceneNetSpecific]
        public void TestGenericCollection_TestIsReadOnly()
        {
            base.TestIsReadOnly();
        }

        [Test, LuceneNetSpecific]
        public void TestGenericCollection_TestGetEnumerator()
        {
            base.TestGetEnumerator();
        }

        [Test, LuceneNetSpecific]
        public void TestGenericCollection_TestGetEnumerator2()
        {
            base.TestGetEnumerator2();
        }

        #endregion

        #region TestCollection<TList, TFactory, TItem>
        // LUCENENET NOTE: Tests in an abstract base class are not pulled into the correct
        // context in Visual Studio. This fixes that with the minimum amount of code necessary
        // to run them in the correct context without duplicating all of the tests.

        [Test, LuceneNetSpecific]
        public void TestCollection_TestAddRemove()
        {
            base.TestAddRemove();
        }

        [Test, LuceneNetSpecific]
        public void TestCollection_TestAddReverseRemove()
        {
            base.TestAddReverseRemove();
        }

        [Test, LuceneNetSpecific]
        public void TestCollection_TestClear()
        {
            base.TestClear();
        }

        [Test, LuceneNetSpecific]
        public void TestCollection_TestContains()
        {
            base.TestContains();
        }

        [Test, LuceneNetSpecific]
        public void TestCollection_TestCopyTo()
        {
            base.TestCopyTo();
        }

        [Test, LuceneNetSpecific]
        public void TestCollection_TestIsReadOnly()
        {
            base.TestIsReadOnly();
        }

        [Test, LuceneNetSpecific]
        public void TestCollection_TestGetEnumerator()
        {
            base.TestGetEnumerator();
        }

        [Test, LuceneNetSpecific]
        public void TestCollection_TestGetEnumerator2()
        {
            base.TestGetEnumerator2();
        }

        #endregion

    }

    [TestFixture]
    public class TestLurchTableDictionary : TestDictionary<LurchTable<Guid, String>, TestLurchTableDictionary.Factory, Guid, String>
    {
        private const int SAMPLE_SIZE = 1000;
        public new class Factory : IFactory<LurchTable<Guid, String>>
        {
            public LurchTable<Guid, string> Create()
            {
                return new LurchTable<Guid, string>(SAMPLE_SIZE, LurchTableOrder.Access);
            }
        }

        protected override KeyValuePair<Guid, string>[] GetSample()
        {
            var results = new List<KeyValuePair<Guid, string>>();
            for (int i = 0; i < SAMPLE_SIZE; i++)
            {
                Guid id = Guid.NewGuid();
                results.Add(new KeyValuePair<Guid, string>(id, id.ToString()));
            }
            return results.ToArray();
        }


        #region TestDictionary<TDictionary, TFactory, TKey, TValue>
        // LUCENENET NOTE: Tests in an abstract base class are not pulled into the correct
        // context in Visual Studio. This fixes that with the minimum amount of code necessary
        // to run them in the correct context without duplicating all of the tests.

        [Test, LuceneNetSpecific]
        public void TestDictionary_TestAddRemoveByKey()
        {
            base.TestAddRemoveByKey();
        }

        [Test, LuceneNetSpecific]
        public void TestDictionary_TestKeys()
        {
            base.TestKeys();
        }

        [Test, LuceneNetSpecific]
        public void TestDictionary_TestValues()
        {
            base.TestValues();
        }

        #endregion
    }


    #region Support

    #region Abstract classes
    public abstract class TestGenericCollection<TList, TItem>
        where TList : ICollection<TItem>, new()
    {
        protected abstract TItem[] GetSample();

        protected TList CreateSample(TItem[] items)
        {
            TList list = new TList();

            int count = 0;
            Assert.AreEqual(count, list.Count);

            foreach (TItem item in items)
            {
                list.Add(item);
                Assert.AreEqual(++count, list.Count);
            }
            return list;
        }

        // [Test, LuceneNetSpecific] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestAddRemove()
        {
            TList list = new TList();
            TItem[] items = GetSample();

            int count = 0;
            Assert.AreEqual(count, list.Count);

            foreach (TItem item in items)
            {
                list.Add(item);
                Assert.AreEqual(++count, list.Count);
            }
            foreach (TItem item in items)
            {
                Assert.IsTrue(list.Remove(item));
                Assert.AreEqual(--count, list.Count);
            }
        }

        // [Test, LuceneNetSpecific] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestAddReverseRemove()
        {
            TList list = new TList();
            TItem[] items = GetSample();

            int count = 0;
            Assert.AreEqual(count, list.Count);

            foreach (TItem item in items)
            {
                list.Add(item);
                Assert.AreEqual(++count, list.Count);
            }
            for (int ix = items.Length - 1; ix >= 0; ix--)
            {
                Assert.IsTrue(list.Remove(items[ix]));
                Assert.AreEqual(--count, list.Count);
            }
        }

        // [Test, LuceneNetSpecific] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestClear()
        {
            TList list = new TList();
            TItem[] items = GetSample();

            foreach (TItem item in items)
                list.Add(item);
            Assert.AreEqual(items.Length, list.Count);

            Assert.AreNotEqual(0, list.Count);
            list.Clear();
            Assert.AreEqual(0, list.Count);
        }

        // [Test, LuceneNetSpecific] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestContains()
        {
            TList list = new TList();
            TItem[] items = GetSample();

            foreach (TItem item in items)
                list.Add(item);
            Assert.AreEqual(items.Length, list.Count);

            foreach (TItem item in items)
                Assert.IsTrue(list.Contains(item));
        }

        // [Test, LuceneNetSpecific] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestCopyTo()
        {
            TList list = new TList();
            List<TItem> items = new List<TItem>(GetSample());

            foreach (TItem item in items)
                list.Add(item);
            Assert.AreEqual(items.Count, list.Count);

            TItem[] copy = new TItem[items.Count + 1];
            list.CopyTo(copy, 1);
            Assert.AreEqual(default(TItem), copy[0]);

            for (int i = 1; i < copy.Length; i++)
                Assert.IsTrue(items.Remove(copy[i]));

            Assert.AreEqual(0, items.Count);
        }

        // [Test, LuceneNetSpecific] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestIsReadOnly()
        {
            Assert.IsFalse(new TList().IsReadOnly);
        }

        // [Test, LuceneNetSpecific] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestGetEnumerator()
        {
            TList list = new TList();
            List<TItem> items = new List<TItem>(GetSample());

            foreach (TItem item in items)
                list.Add(item);
            Assert.AreEqual(items.Count, list.Count);

            foreach (TItem item in list)
                Assert.IsTrue(items.Remove(item));

            Assert.AreEqual(0, items.Count);
        }

        // [Test, LuceneNetSpecific] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestGetEnumerator2()
        {
            TList list = new TList();
            List<TItem> items = new List<TItem>(GetSample());

            foreach (TItem item in items)
                list.Add(item);
            Assert.AreEqual(items.Count, list.Count);

            foreach (TItem item in ((System.Collections.IEnumerable)list))
                Assert.IsTrue(items.Remove(item));

            Assert.AreEqual(0, items.Count);
        }

        public static void VerifyCollection<T, TC>(IEqualityComparer<T> comparer, ICollection<T> expected, TC collection) where TC : ICollection<T>
        {
            Assert.AreEqual(expected.IsReadOnly, collection.IsReadOnly);
            Assert.AreEqual(expected.Count, collection.Count);
            CompareEnumerations(comparer, expected, collection);
            using (var a = expected.GetEnumerator())
            using (var b = collection.GetEnumerator())
            {
                bool result;
                Assert.IsTrue(b.MoveNext());
                b.Reset();
                Assert.AreEqual(result = a.MoveNext(), b.MoveNext());
                while (result)
                {
                    Assert.IsTrue(comparer.Equals(a.Current, b.Current));
                    Assert.IsTrue(comparer.Equals(a.Current, (T)((System.Collections.IEnumerator)b).Current));
                    Assert.AreEqual(result = a.MoveNext(), b.MoveNext());
                }
            }

            T[] items = new T[10 + collection.Count];
            collection.CopyTo(items, 5);
            Array.Copy(items, 5, items, 0, collection.Count);
            Array.Resize(ref items, collection.Count);
            CompareEnumerations(comparer, expected, collection);

            for (int i = 0; i < 5; i++)
                Assert.IsTrue(collection.Contains(items[i]));
        }

        public static void CompareEnumerations<T>(IEqualityComparer<T> comparer, IEnumerable<T> expected, IEnumerable<T> collection)
        {
            using (var a = expected.GetEnumerator())
            using (var b = collection.GetEnumerator())
            {
                bool result;
                Assert.AreEqual(result = a.MoveNext(), b.MoveNext());
                while (result)
                {
                    Assert.IsTrue(comparer.Equals(a.Current, b.Current));
                    Assert.AreEqual(result = a.MoveNext(), b.MoveNext());
                }
            }
        }
    }

    public abstract class TestDictionary<TDictionary, TFactory, TKey, TValue> : TestCollection<TDictionary, TFactory, KeyValuePair<TKey, TValue>>
        where TDictionary : IDictionary<TKey, TValue>, IDisposable
        where TFactory : IFactory<TDictionary>, new()
    {
        // [Test, LuceneNetSpecific] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestAddRemoveByKey()
        {
            KeyValuePair<TKey, TValue>[] sample = GetSample();

            using (TDictionary test = Factory.Create())
            {
                foreach (KeyValuePair<TKey, TValue> kv in sample)
                    test.Add(kv.Key, kv.Value);

                foreach (KeyValuePair<TKey, TValue> kv in sample)
                    Assert.IsTrue(test.ContainsKey(kv.Key));

                TValue cmp;
                foreach (KeyValuePair<TKey, TValue> kv in sample)
                    Assert.IsTrue(test.TryGetValue(kv.Key, out cmp) && kv.Value.Equals(cmp));

                foreach (KeyValuePair<TKey, TValue> kv in sample)
                    Assert.IsTrue(test.Remove(kv.Key));
            }
        }

        // [Test, LuceneNetSpecific] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestKeys()
        {
            KeyValuePair<TKey, TValue>[] sample = GetSample();

            using (TDictionary test = Factory.Create())
            {
                List<TKey> keys = new List<TKey>();

                foreach (KeyValuePair<TKey, TValue> kv in sample)
                {
                    test[kv.Key] = kv.Value;
                    keys.Add(kv.Key);
                }

                List<TKey> cmp = new List<TKey>(test.Keys);

                Assert.AreEqual(keys.Count, cmp.Count);
                for (int i = 0; i < keys.Count; i++)
                    Assert.IsTrue(test.ContainsKey(keys[i]));
            }
        }

        // [Test, LuceneNetSpecific] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestValues()
        {
            KeyValuePair<TKey, TValue>[] sample = GetSample();

            using (TDictionary test = Factory.Create())
            {
                List<TValue> values = new List<TValue>();

                foreach (KeyValuePair<TKey, TValue> kv in sample)
                {
                    test[kv.Key] = kv.Value;
                    values.Add(kv.Value);
                }

                List<TValue> cmp = new List<TValue>(test.Values);
                Assert.AreEqual(values.Count, cmp.Count);
            }
        }
    }

    public abstract class TestCollection<TList, TFactory, TItem>
        where TList : ICollection<TItem>, IDisposable
        where TFactory : IFactory<TList>, new()
    {
        protected abstract TItem[] GetSample();

        protected readonly TFactory Factory = new TFactory();

        // [Test, LuceneNetSpecific] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestAddRemove()
        {
            using (TList list = Factory.Create())
            {
                TItem[] items = GetSample();

                int count = 0;
                Assert.AreEqual(count, list.Count);

                foreach (TItem item in items)
                {
                    list.Add(item);
                    Assert.AreEqual(++count, list.Count);
                }
                foreach (TItem item in items)
                {
                    Assert.IsTrue(list.Remove(item));
                    Assert.AreEqual(--count, list.Count);
                }
            }
        }

        // [Test, LuceneNetSpecific] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestAddReverseRemove()
        {
            using (TList list = Factory.Create())
            {
                TItem[] items = GetSample();

                int count = 0;
                Assert.AreEqual(count, list.Count);

                foreach (TItem item in items)
                {
                    list.Add(item);
                    Assert.AreEqual(++count, list.Count);
                }
                for (int ix = items.Length - 1; ix >= 0; ix--)
                {
                    Assert.IsTrue(list.Remove(items[ix]));
                    Assert.AreEqual(--count, list.Count);
                }
            }
        }

        // [Test, LuceneNetSpecific] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestClear()
        {
            using (TList list = Factory.Create())
            {
                TItem[] items = GetSample();

                foreach (TItem item in items)
                    list.Add(item);
                Assert.AreEqual(items.Length, list.Count);

                Assert.AreNotEqual(0, list.Count);
                list.Clear();
                Assert.AreEqual(0, list.Count);
            }
        }

        // [Test, LuceneNetSpecific] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestContains()
        {
            using (TList list = Factory.Create())
            {
                TItem[] items = GetSample();

                foreach (TItem item in items)
                    list.Add(item);
                Assert.AreEqual(items.Length, list.Count);

                foreach (TItem item in items)
                    Assert.IsTrue(list.Contains(item));
            }
        }

        // [Test, LuceneNetSpecific] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestCopyTo()
        {
            using (TList list = Factory.Create())
            {
                List<TItem> items = new List<TItem>(GetSample());

                foreach (TItem item in items)
                    list.Add(item);
                Assert.AreEqual(items.Count, list.Count);

                TItem[] copy = new TItem[items.Count + 1];
                list.CopyTo(copy, 1);
                Assert.AreEqual(default(TItem), copy[0]);

                for (int i = 1; i < copy.Length; i++)
                    Assert.IsTrue(items.Remove(copy[i]));

                Assert.AreEqual(0, items.Count);
            }
        }

        // [Test, LuceneNetSpecific] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestIsReadOnly()
        {
            using (TList list = Factory.Create())
                Assert.IsFalse(list.IsReadOnly);
        }

        // [Test, LuceneNetSpecific] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestGetEnumerator()
        {
            using (TList list = Factory.Create())
            {
                List<TItem> items = new List<TItem>(GetSample());

                foreach (TItem item in items)
                    list.Add(item);
                Assert.AreEqual(items.Count, list.Count);

                foreach (TItem item in list)
                    Assert.IsTrue(items.Remove(item));

                Assert.AreEqual(0, items.Count);
            }
        }

        // [Test, LuceneNetSpecific] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestGetEnumerator2()
        {
            using (TList list = Factory.Create())
            {
                List<TItem> items = new List<TItem>(GetSample());

                foreach (TItem item in items)
                    list.Add(item);
                Assert.AreEqual(items.Count, list.Count);

                foreach (TItem item in ((System.Collections.IEnumerable)list))
                    Assert.IsTrue(items.Remove(item));

                Assert.AreEqual(0, items.Count);
            }
        }
    }

    #endregion

    #region Interfaces

    /// <summary> Generic factory for instances of type T </summary>
    public interface IFactory<T>
    {
        /// <summary> Creates an instance of an object assignable to type T </summary>
        T Create();
    }

    #endregion

    #endregion
}
