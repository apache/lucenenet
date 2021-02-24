// Lucene version compatibility level 4.10.4
using NUnit.Framework;

namespace Lucene.Net.Analysis.Hunspell
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

    public class TestCaseSensitive : StemmerTestBase
    {
        public override void BeforeClass()
        {
            base.BeforeClass();
            Init("casesensitive.aff", "casesensitive.dic");
        }

        [Test]
        public void TestAllPossibilities()
        {
            AssertStemsTo("drink", "drink");
            AssertStemsTo("drinks", "drink");
            AssertStemsTo("drinkS", "drink");
            AssertStemsTo("gooddrinks", "drink");
            AssertStemsTo("Gooddrinks", "drink", "drink");
            AssertStemsTo("GOODdrinks", "drink");
            AssertStemsTo("gooddrinkS", "drink");
            AssertStemsTo("GooddrinkS", "drink");
            AssertStemsTo("gooddrink", "drink");
            AssertStemsTo("Gooddrink", "drink", "drink");
            AssertStemsTo("GOODdrink", "drink");
            AssertStemsTo("Drink", "drink", "Drink");
            AssertStemsTo("Drinks", "drink", "Drink");
            AssertStemsTo("DrinkS", "Drink");
            AssertStemsTo("goodDrinks", "Drink");
            AssertStemsTo("GoodDrinks", "Drink");
            AssertStemsTo("GOODDrinks", "Drink");
            AssertStemsTo("goodDrinkS", "Drink");
            AssertStemsTo("GoodDrinkS", "Drink");
            AssertStemsTo("GOODDrinkS", "Drink");
            AssertStemsTo("goodDrink", "Drink");
            AssertStemsTo("GoodDrink", "Drink");
            AssertStemsTo("GOODDrink", "Drink");
            AssertStemsTo("DRINK", "DRINK", "drink", "Drink");
            AssertStemsTo("DRINKs", "DRINK");
            AssertStemsTo("DRINKS", "DRINK", "drink", "Drink");
            AssertStemsTo("goodDRINKs", "DRINK");
            AssertStemsTo("GoodDRINKs", "DRINK");
            AssertStemsTo("GOODDRINKs", "DRINK");
            AssertStemsTo("goodDRINKS", "DRINK");
            AssertStemsTo("GoodDRINKS", "DRINK");
            AssertStemsTo("GOODDRINKS", "DRINK", "drink", "drink");
            AssertStemsTo("goodDRINK", "DRINK");
            AssertStemsTo("GoodDRINK", "DRINK");
            AssertStemsTo("GOODDRINK", "DRINK", "drink", "drink");
        }
    }
}
