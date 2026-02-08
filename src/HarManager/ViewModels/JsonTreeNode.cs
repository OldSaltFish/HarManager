using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json.Linq;

namespace HarManager.ViewModels
{
    public partial class JsonTreeNode : ObservableObject
    {
        [ObservableProperty]
        private string _key = string.Empty;

        [ObservableProperty]
        private string _value = string.Empty;

        [ObservableProperty]
        private string _type = string.Empty;

        [ObservableProperty]
        private bool _isExpanded = true;

        public ObservableCollection<JsonTreeNode> Children { get; } = new();

        public JToken Token { get; }

        public JsonTreeNode(string key, JToken token)
        {
            Key = key;
            Token = token;
            Type = token.Type.ToString();

            if (token is JValue value)
            {
                Value = value.ToString();
            }
            else if (token is JObject obj)
            {
                Value = !obj.Properties().Any() ? "{}" : "{ }";
                foreach (var property in obj.Properties())
                {
                    Children.Add(new JsonTreeNode(property.Name, property.Value));
                }
            }
            else if (token is JArray array)
            {
                Value = array.Count == 0 ? "[]" : $"[ {array.Count} ]";
                for (int i = 0; i < array.Count; i++)
                {
                    Children.Add(new JsonTreeNode($"[{i}]", array[i]));
                }
            }
        }
    }
}
