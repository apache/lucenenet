﻿using Lucene.Net.Diagnostics;
using System;

namespace Lucene.Net.Store
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Base implementation for a concrete <see cref="Directory"/>.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public abstract class BaseDirectory : Directory
    {
        private volatile bool isOpen = true;

        // LUCENENET specific - since we can't make a CLS-compliant 
        // protected volatile field, we are exposing it through a protected
        // property.
        protected internal virtual bool IsOpen
        {
            get => isOpen;
            set => isOpen = value;
        }

        /// <summary>
        /// Holds the LockFactory instance (implements locking for
        /// this <see cref="Directory"/> instance).
        /// </summary>
        protected internal LockFactory m_lockFactory;

        /// <summary>
        /// Sole constructor. </summary>
        protected BaseDirectory()
            : base()
        {
        }

        public override Lock MakeLock(string name)
        {
            return m_lockFactory.MakeLock(name);
        }

        public override void ClearLock(string name)
        {
            m_lockFactory?.ClearLock(name);
        }

        public override void SetLockFactory(LockFactory lockFactory)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(lockFactory != null);
            this.m_lockFactory = lockFactory;
            lockFactory.LockPrefix = this.GetLockID();
        }

        public override LockFactory LockFactory => this.m_lockFactory;

        protected internal override sealed void EnsureOpen()
        {
            if (!IsOpen)
            {
                throw AlreadyClosedException.Create(this.GetType().FullName, "this Directory is disposed.");
            }
        }
    }
}