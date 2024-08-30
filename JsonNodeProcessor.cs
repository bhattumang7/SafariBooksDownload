namespace SafariBooksDownload
{
    public class JsonNodeProcessor
    {
        private int currentOrder = 0;
        private int maxDepth = 0;
        private HashSet<string> distinctUrl = new HashSet<string>();
        public void AssignOrder(List<JsonNodeInfo> nodes)
        {
            foreach (var node in nodes)
            {
                distinctUrl.Add(node.url);
                updateMaxDepth(node);
                AssignOrderRecursive(node);
            }
        }

        private void AssignOrderRecursive(JsonNodeInfo node)
        {
            node.order = currentOrder++;
            distinctUrl.Add(node.url);
            if (node.children != null)
            {
                foreach (var child in node.children)
                {
                    updateMaxDepth(child);
                    AssignOrderRecursive(child);
                }
            }
        }

        private void updateMaxDepth(JsonNodeInfo node)
        {
            if (node.depth > maxDepth)
            {
                maxDepth = node.depth;
            }
        }

        public int getMaxDepth()
        {
            return maxDepth;
        }

        public HashSet<string> getUniquePageList()
        {
            return distinctUrl;
        }
    }
}
