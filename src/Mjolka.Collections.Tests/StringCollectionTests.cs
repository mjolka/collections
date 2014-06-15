namespace Mjolka.Collections.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Mjolka.Collections.Tests.Properties;

    using NUnit.Framework;

    [TestFixture]
    public class StringCollectionTests
    {
        [Test]
        public void StringCollectionOfNullThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new StringCollection(null));
        }

        [Test]
        public void StringCollectionContainingNullThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new StringCollection(new string[] { null }));
            Assert.Throws<ArgumentException>(() => new StringCollection(new[] { "a", null }));
        }

        [Test]
        public void StringCollectionOfZeroStringsDoesNotThrow()
        {
            Assert.DoesNotThrow(() => new StringCollection(Enumerable.Empty<string>()));
        }

        [Test]
        public void StringCollectionOfOnlyEmptyString()
        {
            var testCase = new[] { string.Empty };
            var strings = new StringCollection(testCase);
            Assert.IsTrue(strings.Contains(string.Empty));
            Assert.IsFalse(strings.Contains(null));
            Assert.IsFalse(strings.Contains("a"));
            Assert.AreEqual(1, strings.Count);
            CollectionAssert.AreEqual(testCase, strings);
        }

        [Test]
        public void StringCollectionOfEmptyStringAndA()
        {
            var testCase = new[] { string.Empty, "a" };
            var strings = new StringCollection(testCase);
            Assert.IsTrue(strings.Contains(string.Empty));
            Assert.IsTrue(strings.Contains("a"));
            Assert.IsFalse(strings.Contains("b"));
            Assert.AreEqual(testCase.Length, strings.Count);
            CollectionAssert.AreEqual(testCase, strings);
        }

        [Test]
        public void StringCollectionOfBatsCatsAndRatsIsMinimal()
        {
            var testCase = new[] { "bats", "cats", "rats" };
            var strings = new StringCollection(testCase);
            Assert.AreEqual(5, strings.CountStates());
            CollectionAssert.AreEqual(testCase, strings);
        }

        [Test]
        public void StringCollectionEnumeratorMatchesDictionary()
        {
            var testCase = ReadDictionary();
            var strings = new StringCollection(testCase);
            Assert.AreEqual(testCase.Length, strings.Count);
            CollectionAssert.AreEqual(testCase, strings);
        }

        [Test]
        public void StringCollectionEnumeratorWorksAfterReset()
        {
            var testCase = ReadDictionary();
            var strings = new StringCollection(testCase);
            using (IEnumerator<string> enumerator = strings.GetEnumerator())
            {
                foreach (var word in testCase)
                {
                    enumerator.MoveNext();
                    Assert.AreEqual(word, enumerator.Current);
                }
                
                enumerator.Reset();
                foreach (var word in testCase)
                {
                    enumerator.MoveNext();
                    Assert.AreEqual(word, enumerator.Current);
                }
            }
        }

        public static string[] ReadDictionary()
        {
            // Dictionary from http://www.curlewcommunications.co.uk/wordlist.html
            // under the following conditions:
            // These files are provided "as is" without any warranty. They may be used
            // and copied freely for non-commercial purposes provided that this notice
            // is included. Downloading of the files signifies acceptance of these conditions.
            // All rights are reserved.
            var words = new List<string>();
            using (var stringReader = new StringReader(Resources.BritishWords))
            {
                string word;
                while ((word = stringReader.ReadLine()) != null)
                {
                    words.Add(word);
                }
            }

            var wordArray = words.ToArray();
            Array.Sort(wordArray, StringComparer.Ordinal);
            return wordArray;
        }
    }
}
