import { create } from "zustand";
import type { ConnectionProfile } from "../../shared/models/connection";
import type { DatabaseObjectNode } from "../../shared/models/database-object";
import { explorerApi } from "../features/explorer/explorer-api";

interface ExplorerState {
  tree: DatabaseObjectNode[];
  selectedNode: DatabaseObjectNode | null;
  isLoading: boolean;
  error: string;
  loadTree: (profile: ConnectionProfile) => Promise<void>;
  selectNode: (node: DatabaseObjectNode) => void;
  reset: () => void;
}

export const useExplorerStore = create<ExplorerState>((set) => ({
  tree: [],
  selectedNode: null,
  isLoading: false,
  error: "",
  async loadTree(profile) {
    set({ isLoading: true, error: "" });

    try {
      const tree = await explorerApi.loadTree(profile);
      set({ tree, selectedNode: null, isLoading: false });
    } catch {
      set({ tree: [], selectedNode: null, isLoading: false, error: "对象树加载失败" });
    }
  },
  selectNode(node) {
    set({ selectedNode: node });
  },
  reset() {
    set({ tree: [], selectedNode: null, isLoading: false, error: "" });
  }
}));
