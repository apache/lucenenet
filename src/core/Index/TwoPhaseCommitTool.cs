using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public static class TwoPhaseCommitTool
    {
        public class PrepareCommitFailException : System.IO.IOException
        {
            public PrepareCommitFailException(Exception cause, ITwoPhaseCommit obj)
                : base("prepareCommit() failed on " + obj, cause)
            {
            }
        }

        public class CommitFailException : System.IO.IOException
        {
            public CommitFailException(Exception cause, ITwoPhaseCommit obj)
                : base("commit() failed on " + obj, cause)
            {
            }
        }

        private static void Rollback(params ITwoPhaseCommit[] objects)
        {
            foreach (ITwoPhaseCommit tpc in objects)
            {
                // ignore any exception that occurs during rollback - we want to ensure
                // all objects are rolled-back.
                if (tpc != null)
                {
                    try
                    {
                        tpc.Rollback();
                    }
                    catch { }
                }
            }
        }

        public static void Execute(params ITwoPhaseCommit[] objects)
        {
            ITwoPhaseCommit tpc = null;
            try
            {
                // first, all should successfully prepareCommit()
                for (int i = 0; i < objects.Length; i++)
                {
                    tpc = objects[i];
                    if (tpc != null)
                    {
                        tpc.PrepareCommit();
                    }
                }
            }
            catch (Exception t)
            {
                // first object that fails results in rollback all of them and
                // throwing an exception.
                Rollback(objects);
                throw new PrepareCommitFailException(t, tpc);
            }

            // If all successfully prepareCommit(), attempt the actual commit()
            try
            {
                for (int i = 0; i < objects.Length; i++)
                {
                    tpc = objects[i];
                    if (tpc != null)
                    {
                        tpc.Commit();
                    }
                }
            }
            catch (Exception t)
            {
                // first object that fails results in rollback all of them and
                // throwing an exception.
                Rollback(objects);
                throw new CommitFailException(t, tpc);
            }
        }
    }
}
