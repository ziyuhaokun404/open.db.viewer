import "@testing-library/jest-dom/vitest";

const defaultInvoke = (async (channel: string) => {
  if (channel === "connection:test") {
    return {
      success: false,
      message: "Not implemented in test setup."
    };
  }

  return [];
}) as Window["electron"]["ipcRenderer"]["invoke"];

window.electron = {
  ipcRenderer: {
    invoke: defaultInvoke
  }
};
