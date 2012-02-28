using NUnit.Framework;

namespace Lucene.Net.Support
{
    [TestFixture]
    public class TestThreadClass
    {
        [Test]
        public void Test()
        {
            ThreadClass thread = new ThreadClass();

            //Compare Current Thread Ids
            Assert.IsTrue(ThreadClass.Current().Instance.ManagedThreadId == System.Threading.Thread.CurrentThread.ManagedThreadId);


            //Compare instances of ThreadClass
            MyThread mythread = new MyThread();
            mythread.Start();
            while (mythread.Result == null) System.Threading.Thread.Sleep(1);
            Assert.IsTrue((bool)mythread.Result);


            ThreadClass nullThread = null;
            Assert.IsTrue(nullThread == null); //test overloaded operator == with null values
            Assert.IsFalse(nullThread != null); //test overloaded operator != with null values
        }

        class MyThread : ThreadClass
        {
            public object Result = null;
            public override void Run()
            {
                Result = ThreadClass.Current() == this;
            }
        }
    }
}