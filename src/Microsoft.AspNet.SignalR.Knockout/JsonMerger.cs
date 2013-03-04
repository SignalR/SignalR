// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Knockout
{
    public class JsonMerger
    {
        private JToken _state;

        public JsonMerger(JToken state)
        {
            // We don't want anyone else holding this reference. That's just asking for problems.
            _state = state.DeepClone();
        }

        public JToken State
        {
            get
            {
                lock (_state)
                {
                    return _state.DeepClone();
                }
            }
        }

        public void Merge(JToken diff)
        {
            lock (_state)
            {
                Merge(ref _state, diff);
            }
        }

        private void Merge(ref JToken state, JToken diff)
        {
            if (IsPrimitive(state) || diff.Type != JTokenType.Object)           {
                state = diff;
            }
            else
            {
                var diffObject = (JObject)diff;
                var replaced = diff.Value<bool?>("_replaced");

                if (replaced.HasValue && replaced.Value)
                {
                    state["_tag"] = diff["_tag"];
                    state["value"] = diff["value"];
                }
                else
                {
                    switch (state.Type)
                    {
                        case JTokenType.Object:
                            Merge((JObject)state, diffObject);
                            break;
                        case JTokenType.Array:
                            Merge((JArray)state, diffObject);
                            break;
                    }
                }
            }
        }

        private void Merge(JObject state, JObject diff)
        {
            foreach (KeyValuePair<string, JToken> pair in diff)
            {
                if (IsDeleted(pair.Value))
                {
                    state.Remove(pair.Key);
                }
                // The update tag is only useful to clients receiving the
                // diff it is included in.
                else if (pair.Key != "_updated")
                {
                    JToken innerState = state[pair.Key] ?? new JObject();
                    Merge(ref innerState, pair.Value);
                    state[pair.Key] = innerState;
                }
            }
        }

        private void Merge(JArray state, JObject diff)
        {
            // TODO: include array length in diff so we can just resize it.
            // I'm not sure if you can guarantee ordering when iterating over a JObject
            var indicesToRemove = new List<int>();
            var valuesToReplaceOrAdd = new SortedList<int, JToken>();

            foreach (KeyValuePair<string, JToken> pair in diff)
            {
                int index;
                if (int.TryParse(pair.Key, out index))
                {
                    if (IsDeleted(pair.Value))
                    {
                        indicesToRemove = indicesToRemove ?? new List<int>();
                        indicesToRemove.Add(index);
                    }
                    else
                    {
                        valuesToReplaceOrAdd.Add(index, pair.Value);
                    }
                }
            }

            // Add forward
            foreach (KeyValuePair<int, JToken> pair in valuesToReplaceOrAdd)
            {
                if (pair.Key < state.Count)
                {
                    JToken innerState = state[pair.Key];
                    Merge(ref innerState, pair.Value);
                    state[pair.Key] = innerState;
                }
                else if (pair.Key == state.Count)
                {
                    state.Insert(pair.Key, pair.Value);
                }
            }

            // Remove backward
            indicesToRemove.Sort();
            indicesToRemove.Reverse();
            foreach (var index in indicesToRemove)
            {
                state.RemoveAt(index);
            }
        }

        private static bool IsDeleted(JToken value)
        {
            return value.Type == JTokenType.Object &&
                   value.Value<string>("_tag") == "delete";
        }

        private static bool IsPrimitive(JToken value)
        {
            return value.Type != JTokenType.Object && value.Type != JTokenType.Array;
        }
    }
}
