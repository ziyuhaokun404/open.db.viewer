import { create } from "zustand";
import type { ConnectionProfile, DatabaseType, TestConnectionResult } from "../../shared/models/connection";
import { connectionApi } from "../features/connections/connection-api";

interface ConnectionFormValues {
  id: string;
  type: DatabaseType;
  name: string;
  host: string;
  port: string;
  username: string;
  password: string;
  database: string;
  filePath: string;
}

interface ConnectionState {
  savedConnections: ConnectionProfile[];
  activeConnection: ConnectionProfile | null;
  isLoadingList: boolean;
  isTestingConnection: boolean;
  saveMessage: string;
  testResult: TestConnectionResult | null;
  currentView: "home" | "new-connection" | "browser";
  formValues: ConnectionFormValues;
  loadConnections: () => Promise<void>;
  openNewConnection: () => void;
  openConnection: (connection: ConnectionProfile) => void;
  goHome: () => void;
  reset: () => void;
  updateForm: (field: keyof ConnectionFormValues, value: string) => void;
  testConnection: () => Promise<void>;
  saveConnection: () => Promise<void>;
}

function createDefaultFormValues(): ConnectionFormValues {
  return {
    id: crypto.randomUUID(),
    type: "sqlite",
    name: "",
    host: "",
    port: "",
    username: "",
    password: "",
    database: "",
    filePath: ""
  };
}

function toConnectionProfile(values: ConnectionFormValues): ConnectionProfile {
  return {
    id: values.id,
    type: values.type,
    name: values.name,
    host: values.host || undefined,
    port: values.port ? Number(values.port) : undefined,
    username: values.username || undefined,
    password: values.password || undefined,
    database: values.database || undefined,
    filePath: values.filePath || undefined
  };
}

export const useConnectionStore = create<ConnectionState>((set, get) => ({
  savedConnections: [],
  activeConnection: null,
  isLoadingList: false,
  isTestingConnection: false,
  saveMessage: "",
  testResult: null,
  currentView: "home",
  formValues: createDefaultFormValues(),
  async loadConnections() {
    set({ isLoadingList: true });
    const savedConnections = await connectionApi.listConnections();
    set({ savedConnections, isLoadingList: false });
  },
  openNewConnection() {
    set({
      currentView: "new-connection",
      formValues: createDefaultFormValues(),
      testResult: null,
      saveMessage: ""
    });
  },
  openConnection(connection) {
    set({
      activeConnection: connection,
      currentView: "browser",
      saveMessage: ""
    });
  },
  goHome() {
    set({
      activeConnection: null,
      currentView: "home",
      testResult: null,
      saveMessage: ""
    });
  },
  reset() {
    set({
      savedConnections: [],
      activeConnection: null,
      isLoadingList: false,
      isTestingConnection: false,
      saveMessage: "",
      testResult: null,
      currentView: "home",
      formValues: createDefaultFormValues()
    });
  },
  updateForm(field, value) {
    set((state) => ({
      formValues: {
        ...state.formValues,
        [field]: value
      },
      testResult: field === "type" ? null : state.testResult,
      saveMessage: ""
    }));
  },
  async testConnection() {
    set({ isTestingConnection: true, testResult: null });
    const profile = toConnectionProfile(get().formValues);
    const testResult = await connectionApi.testConnection(profile);
    set({ isTestingConnection: false, testResult });
  },
  async saveConnection() {
    const profile = toConnectionProfile(get().formValues);
    await connectionApi.saveConnection(profile);
    const savedConnections = await connectionApi.listConnections();
    set({
      savedConnections,
      currentView: "home",
      saveMessage: `${profile.name} 已保存`,
      testResult: null,
      formValues: createDefaultFormValues()
    });
  }
}));
