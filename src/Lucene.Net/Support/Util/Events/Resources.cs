// Source: https://github.com/PrismLibrary/Prism/blob/7f0b1680bbe754da790274f80851265f808d9bbf

#region Copyright .NET Foundation, Licensed under the MIT License (MIT)
// The MIT License (MIT)
//
// Copyright(c).NET Foundation
//
// All rights reserved. Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software
// is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
// IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
#endregion

#if !FEATURE_CONDITIONALWEAKTABLE_ENUMERATOR

namespace Lucene.Net.Util.Events
{
    internal static class Resources
    {
        public const string EventAggregatorNotConstructedOnUIThread = "To use the UIThread option for subscribing, the EventAggregator must be constructed on the UI thread.";
        public const string InvalidDelegateRerefenceTypeException = "Invalid Delegate Reference Type Exception";
    }
}

#endif
