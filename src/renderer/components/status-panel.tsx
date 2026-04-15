import type { CSSProperties, ReactNode } from "react";

type StatusVariant = "loading" | "empty" | "error" | "success";

const STATUS_LABELS: Record<StatusVariant, string> = {
  loading: "加载中",
  empty: "暂无内容",
  error: "发生错误",
  success: "操作成功"
};

const STATUS_STYLES: Record<StatusVariant, CSSProperties> = {
  loading: {
    backgroundColor: "#f3f4f6",
    borderColor: "#d1d5db",
    color: "#374151"
  },
  empty: {
    backgroundColor: "#f9fafb",
    borderColor: "#e5e7eb",
    color: "#4b5563"
  },
  error: {
    backgroundColor: "#fef2f2",
    borderColor: "#fecaca",
    color: "#991b1b"
  },
  success: {
    backgroundColor: "#ecfdf5",
    borderColor: "#a7f3d0",
    color: "#065f46"
  }
};

export function StatusPanel({
  variant,
  title,
  message,
  action
}: {
  variant: StatusVariant;
  title?: string;
  message: string;
  action?: ReactNode;
}) {
  return (
    <section
      aria-live="polite"
      aria-label={title ?? STATUS_LABELS[variant]}
      style={{
        border: "1px solid",
        borderRadius: "10px",
        padding: "12px 14px",
        margin: "8px 0",
        ...STATUS_STYLES[variant]
      }}
    >
      <strong style={{ display: "block", marginBottom: "6px" }}>{title ?? STATUS_LABELS[variant]}</strong>
      <p style={{ margin: 0 }}>{message}</p>
      {action ? <div>{action}</div> : null}
    </section>
  );
}
