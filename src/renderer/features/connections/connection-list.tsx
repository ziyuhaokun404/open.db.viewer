import type { ConnectionProfile } from "../../../shared/models/connection";

interface ConnectionListProps {
  connections: ConnectionProfile[];
  onOpen: (connection: ConnectionProfile) => void;
}

export function ConnectionList({ connections, onOpen }: ConnectionListProps) {
  if (connections.length === 0) {
    return <p>还没有保存的连接</p>;
  }

  return (
    <ul className="connection-list">
      {connections.map((connection) => (
        <li className="connection-list__item" key={connection.id}>
          <div className="connection-list__meta">
            <p className="connection-list__type">{connection.type}</p>
            <strong className="connection-list__name">{connection.name}</strong>
          </div>
          <button
            aria-label={`打开 ${connection.name}`}
            className="connection-list__open"
            onClick={() => onOpen(connection)}
            type="button"
          >
            进入
          </button>
        </li>
      ))}
    </ul>
  );
}
