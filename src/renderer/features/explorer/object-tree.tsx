import type { DatabaseObjectNode } from "../../../shared/models/database-object";

function TreeNode({
  node,
  onSelect
}: {
  node: DatabaseObjectNode;
  onSelect: (node: DatabaseObjectNode) => void;
}) {
  return (
    <li>
      {node.kind === "table" ? (
        <button onClick={() => onSelect(node)} type="button">
          选择 {node.name}
        </button>
      ) : (
        <span>{node.name}</span>
      )}
      {node.children && node.children.length > 0 ? (
        <ul>
          {node.children.map((child) => (
            <TreeNode key={child.id} node={child} onSelect={onSelect} />
          ))}
        </ul>
      ) : null}
    </li>
  );
}

export function ObjectTree({
  nodes,
  onSelect
}: {
  nodes: DatabaseObjectNode[];
  onSelect: (node: DatabaseObjectNode) => void;
}) {
  if (nodes.length === 0) {
    return <p>当前连接还没有可显示的对象</p>;
  }

  return (
    <ul>
      {nodes.map((node) => (
        <TreeNode key={node.id} node={node} onSelect={onSelect} />
      ))}
    </ul>
  );
}
