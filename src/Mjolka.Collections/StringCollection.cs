// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StringCollection.cs" company="https://github.com/mjolka">
//   Copyright (c) 2014 Matt Collins. Licensed under the MIT License.
//   See LICENSE.txt in the project root for license information.
// </copyright>
// <summary>
//   Defines the StringCollection type.
//   Based on the paper, "Incremental construction of minimal acyclic finite-state automata", by
//   Jan Daciuk, Bruce W. Watson, Stoyan Mihov, and Richard E. Watson.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Mjolka.Collections
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Represents a memory-efficient collection of strings.
    /// </summary>
    [Serializable]
    public class StringCollection : IEnumerable<string>
    {
        /// <summary>
        /// The initial state of the finite-state automaton.
        /// </summary>
        private readonly State initialState;

        /// <summary>
        /// The number of elements in the collection.
        /// </summary>
        private readonly int count;

        /// <summary>
        /// Initializes a new instance of the <see cref="StringCollection"/> class that contains
        /// the specified strings, where the strings are in lexicographic order.
        /// </summary>
        /// <param name="strings">
        /// The collection of strings that are in the new <see cref="StringCollection"/>, in
        /// lexicographic order.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="strings"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An item in <paramref name="strings"/> is <c>null</c>.
        /// </exception>
        public StringCollection(IEnumerable<string> strings)
        {
            if (strings == null)
            {
                throw new ArgumentNullException("strings");
            }

            var register = new Dictionary<State, State>(new StateComparer());
            using (var enumerator = strings.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                {
                    return;
                }

                if (enumerator.Current == null)
                {
                    throw new ArgumentException("strings cannot contain null", "strings");
                }

                if (string.IsNullOrEmpty(enumerator.Current))
                {
                    this.initialState = new FinalState();
                }
                else
                {
                    this.initialState = new State();
                    AddSuffix(this.initialState, enumerator.Current.ToCharArray());
                }

                this.count = 1;

                while (enumerator.MoveNext())
                {
                    var word = enumerator.Current;
                    if (word == null)
                    {
                        throw new ArgumentException("strings cannot contain null", "strings");
                    }

                    var chars = word.ToCharArray();
                    int prefix;
                    var lastState = this.CommonPrefix(chars, out prefix);
                    if (lastState.HasChildren)
                    {
                        ReplaceOrRegister(lastState, register);
                    }

                    var suffix = new ArraySegment<char>(chars, prefix, word.Length - prefix);
                    AddSuffix(lastState, suffix);
                    this.count++;
                }
            }

            ReplaceOrRegister(this.initialState, register);
            register.Clear();
        }

        /// <summary>
        /// Gets the number of strings contained in the <see cref="StringCollection"/>.
        /// </summary>
        public int Count
        {
            get { return this.count; }
        }

        /// <summary>
        /// Determines if the <see cref="StringCollection"/> contains the specified value.
        /// </summary>
        /// <param name="value">
        /// The value to locate in the <see cref="StringCollection"/>.
        /// </param>
        /// <returns>
        /// <code>true</code> if the <see cref="StringCollection"/> contains the specified value;
        /// otherwise, <code>false</code>.
        /// </returns>
        public bool Contains(string value)
        {
            if (value == null)
            {
                return false;
            }

            var state = this.initialState;
            foreach (var c in value.ToCharArray())
            {
                var nextState = state.Transition(c);
                if (nextState == null)
                {
                    return false;
                }

                state = nextState;
            }

            return state.IsFinal;
        }

        /// <summary>
        /// The get enumerator.
        /// </summary>
        /// <returns>
        /// The <see cref="Enumerator"/>.
        /// </returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// The get enumerator.
        /// </summary>
        /// <returns>
        /// The enumerator.
        /// </returns>
        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// The get enumerator.
        /// </summary>
        /// <returns>
        /// The enumerator.
        /// </returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// Counts the number of states in the finite-state automaton.
        /// </summary>
        /// <returns>
        /// The number of states in the finite-state automaton.
        /// </returns>
        internal int CountStates()
        {
            var seen = new HashSet<State>();
            var stack = new Stack<State>();
            stack.Push(this.initialState);
            while (stack.Count > 0)
            {
                var state = stack.Pop();
                if (!seen.Add(state))
                {
                    continue;
                }

                foreach (var child in state.Children)
                {
                    stack.Push(child.State);
                }
            }

            return seen.Count;
        }

        /// <summary>
        /// Adds a suffix to a state.
        /// </summary>
        /// <param name="state">
        /// The state to add the suffix to.
        /// </param>
        /// <param name="suffix">
        /// The suffix to add to the state.
        /// </param>
        private static void AddSuffix(State state, IList<char> suffix)
        {
            var length = suffix.Count - 1;
            for (var i = 0; i < length; i++)
            {
                var newState = new State();
                state.AddEdge(suffix[i], newState);
                state = newState;
            }

            state.AddEdge(suffix[length], new FinalState());
        }

        /// <summary>
        /// Replaces a state or inserts it into the register.
        /// </summary>
        /// <param name="state">
        /// The state to replace or insert into the register.
        /// </param>
        /// <param name="register">
        /// The register.
        /// </param>
        private static void ReplaceOrRegister(State state, IDictionary<State, State> register)
        {
            var child = state.LastChild;
            if (child == null)
            {
                return;
            }

            if (child.HasChildren)
            {
                ReplaceOrRegister(child, register);
            }

            State replacement;
            if (register.TryGetValue(child, out replacement))
            {
                state.LastChild = replacement;
            }
            else
            {
                register.Add(child, child);
            }
        }

        /// <summary>
        /// Gets the common prefix of a word and the finite-state automaton.
        /// </summary>
        /// <param name="word">
        /// The word to find the common prefix of.
        /// </param>
        /// <param name="prefixLength">
        /// The length of the common prefix.
        /// </param>
        /// <returns>
        /// The last <see cref="State"/> in the common prefix.
        /// </returns>
        private State CommonPrefix(IList<char> word, out int prefixLength)
        {
            var state = this.initialState;
            var length = word.Count;
            for (var i = 0; i < length; i++)
            {
                var next = state.Transition(word[i]);
                if (next == null)
                {
                    prefixLength = i;
                    return state;
                }

                state = next;
            }

            prefixLength = length;
            return state;
        }

        /// <summary>
        /// Represents a labeled edge from one state to another.
        /// </summary>
        [Serializable]
        private struct Edge
        {
            /// <summary>
            /// The edge label.
            /// </summary>
            public readonly char Label;

            /// <summary>
            /// The edge state.
            /// </summary>
            public readonly State State;

            /// <summary>
            /// Initializes a new instance of the <see cref="Edge"/> struct.
            /// </summary>
            /// <param name="label">
            /// The edge label.
            /// </param>
            /// <param name="state">
            /// The edge state.
            /// </param>
            public Edge(char label, State state)
            {
                this.Label = label;
                this.State = state;
            }
        }

        /// <summary>
        /// The path segment.
        /// </summary>
        private struct PathSegment
        {
            /// <summary>
            /// The label.
            /// </summary>
            public readonly char Label;

            /// <summary>
            /// The depth.
            /// </summary>
            public readonly int Depth;

            /// <summary>
            /// Initializes a new instance of the <see cref="PathSegment"/> struct.
            /// </summary>
            /// <param name="label">
            /// The label.
            /// </param>
            /// <param name="depth">
            /// The depth.
            /// </param>
            public PathSegment(char label, int depth)
            {
                this.Label = label;
                this.Depth = depth;
            }
        }

        /// <summary>
        /// The enumerator.
        /// </summary>
        public class Enumerator : IEnumerator<string>
        {
            /// <summary>
            /// The initial state.
            /// </summary>
            private readonly State initialState;

            /// <summary>
            /// The string builder.
            /// </summary>
            private readonly StringBuilder stringBuilder;

            /// <summary>
            /// The states to traverse next.
            /// </summary>
            private readonly Stack<State> states;

            /// <summary>
            /// The edges leading to the states on the stack.
            /// </summary>
            private readonly Stack<PathSegment> edges;

            /// <summary>
            /// Initializes a new instance of the <see cref="Enumerator"/> class.
            /// </summary>
            /// <param name="stringCollection">
            /// The string collection.
            /// </param>
            public Enumerator(StringCollection stringCollection)
            {
                this.initialState = stringCollection.initialState;
                this.stringBuilder = new StringBuilder();
                this.states = new Stack<State>();
                this.states.Push(this.initialState);
                this.edges = new Stack<PathSegment>();
            }

            /// <inheritdoc />
            public string Current
            {
                get { return this.stringBuilder.ToString(); }
            }

            /// <inheritdoc />
            object System.Collections.IEnumerator.Current
            {
                get { return this.Current; }
            }

            /// <inheritdoc />
            public bool MoveNext()
            {
                // Move to the next FinalState, adding chars in the
                // path to this.stringBuilder.
                while (this.states.Count > 0)
                {
                    var state = this.states.Pop();
                    var depth = -1;
                    if (this.edges.Count > 0)
                    {
                        var edge = this.edges.Pop();
                        depth = edge.Depth;
                        if (depth < this.stringBuilder.Length)
                        {
                            this.stringBuilder.Remove(depth, this.stringBuilder.Length - depth);
                        }

                        var label = edge.Label;
                        this.stringBuilder.Append(label);
                    }

                    var children = state.Children;
                    if (children != null)
                    {
                        depth = depth + 1;
                        for (var i = children.Length - 1; i >= 0; i--)
                        {
                            var child = children[i];
                            this.edges.Push(new PathSegment(child.Label, depth));
                            this.states.Push(child.State);
                        }
                    }

                    if (state.IsFinal)
                    {
                        return true;
                    }
                }

                return false;
            }

            /// <inheritdoc />
            public void Reset()
            {
                this.stringBuilder.Clear();
                this.states.Clear();
                this.states.Push(this.initialState);
                this.edges.Clear();
            }

            /// <inheritdoc />
            public void Dispose()
            {
            }
        }

        /// <summary>
        /// Represents a state in the finite-state automaton.
        /// </summary>
        [Serializable]
        private class State
        {
            /// <summary>
            /// The edges out of the state.
            /// </summary>
            private Edge[] children = new Edge[0];

            /// <summary>
            /// Gets a value indicating whether the state has children.
            /// </summary>
            public bool HasChildren
            {
                get { return this.children.Length > 0; }
            }

            /// <summary>
            /// Gets or sets the last edge out of the state.
            /// </summary>
            public State LastChild
            {
                get
                {
                    var length = this.children.Length;
                    return length > 0
                        ? this.children[length - 1].State
                        : null;
                }

                set
                {
                    var lastIndex = this.children.Length - 1;
                    var lastChild = this.children[lastIndex];
                    this.children[lastIndex] = new Edge(lastChild.Label, value);
                }
            }

            /// <summary>
            /// Gets a value indicating whether the state is final.
            /// </summary>
            public virtual bool IsFinal
            {
                get { return false; }
            }

            /// <summary>
            /// Gets the children.
            /// </summary>
            public Edge[] Children
            {
                get { return this.children; }
            }

            /// <summary>
            /// Gets the state corresponding to the edge with the specified label out of the state,
            /// if it exists.
            /// </summary>
            /// <param name="label">
            /// The label of the edge out of the state.
            /// </param>
            /// <returns>
            /// The <see cref="State"/> corresponding to the edge with the specified label.
            /// </returns>
            public State Transition(char label)
            {
                var length = this.children.Length;
                for (var i = 0; i < length; i++)
                {
                    var child = this.children[i];
                    if (child.Label == label)
                    {
                        return child.State;
                    }
                }

                return null;
            }

            /// <summary>
            /// Adds an edge out of the state.
            /// </summary>
            /// <param name="label">
            /// The label of the edge to the specified state.
            /// </param>
            /// <param name="state">
            /// The state.
            /// </param>
            public void AddEdge(char label, State state)
            {
                var length = this.children.Length;
                var newChildren = new Edge[length + 1];

                Array.Copy(this.children, newChildren, length);

                newChildren[length] = new Edge(label, state);
                this.children = newChildren;
            }
        }

        /// <summary>
        /// Represents a final state in the finite-state automaton.
        /// </summary>
        [Serializable]
        private sealed class FinalState : State
        {
            /// <inheritdoc />
            public override bool IsFinal
            {
                get { return true; }
            }
        }

        /// <summary>
        /// Represents a state comparer.
        /// </summary>
        private class StateComparer : IEqualityComparer<State>
        {
            /// <inheritdoc />
            public bool Equals(State left, State right)
            {
                if (left.IsFinal != right.IsFinal)
                {
                    return false;
                }

                var leftChildren = left.Children;
                var rightChildren = right.Children;

                if (leftChildren.Length != rightChildren.Length)
                {
                    return false;
                }

                for (var i = 0; i < leftChildren.Length; i++)
                {
                    var leftChild = leftChildren[i];
                    var rightChild = rightChildren[i];
                    if (leftChild.Label != rightChild.Label || leftChild.State != rightChild.State)
                    {
                        return false;
                    }
                }

                return true;
            }

            /// <inheritdoc />
            public int GetHashCode(State obj)
            {
                unchecked
                {
                    var result = obj.IsFinal ? 17 : 521;
                    foreach (var child in obj.Children)
                    {
                        result = (31 * result) + child.Label.GetHashCode();
                        result = (31 * result) + child.State.GetHashCode();
                    }
    
                    return result;
                }
            }
        }
    }
}
