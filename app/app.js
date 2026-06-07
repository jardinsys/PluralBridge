const contractVersion = "phase-3-login-contract";

const mockSession = {
  isAuthenticated: false,
  user: null,
  selectedSystemId: null
};

const demoUser = {
  userId: "mock-user-001",
  username: "demo.user",
  displayName: "Demo User",
  authenticationProvider: "PluralBridge planned username/password contract"
};

const demoSystemAuthorization = {
  userId: "mock-user-001",
  authorizedSystems: [
    {
      systemId: "mock-system-001",
      systemName: "Demo System",
      role: "owner",
      canRead: true,
      canWrite: false,
      selected: true
    }
  ]
};

function baseEnvelope(endpoint, method) {
  return {
    contractVersion: contractVersion,
    endpoint: endpoint,
    method: method,
    transport: "planned REST API",
    mockOnly: true,
    canWrite: false,
    usesPrivateData: false
  };
}

function currentSessionSummary() {
  return {
    isAuthenticated: mockSession.isAuthenticated,
    user: mockSession.user,
    selectedSystemId: mockSession.selectedSystemId
  };
}

function loginSuccess(username) {
  mockSession.isAuthenticated = true;
  mockSession.user = Object.assign({}, demoUser, {
    username: username || demoUser.username
  });
  mockSession.selectedSystemId = "mock-system-001";

  return Object.assign({}, baseEnvelope("/api/session/login", "POST"), {
    result: "authenticated",
    session: currentSessionSummary(),
    userToSystemMapping: demoSystemAuthorization,
    note: "Authentication creates a user session first. System access is supplied by explicit authorization mapping."
  });
}

function loginFailure() {
  mockSession.isAuthenticated = false;
  mockSession.user = null;
  mockSession.selectedSystemId = null;

  return Object.assign({}, baseEnvelope("/api/session/login", "POST"), {
    result: "rejected",
    errorCode: "INVALID_CREDENTIALS",
    session: currentSessionSummary(),
    userToSystemMapping: {
      userId: null,
      authorizedSystems: []
    },
    note: "Failed authentication yields no user identity and no System authorization."
  });
}

function logout() {
  mockSession.isAuthenticated = false;
  mockSession.user = null;
  mockSession.selectedSystemId = null;

  return Object.assign({}, baseEnvelope("/api/session/logout", "POST"), {
    result: "signed out",
    session: currentSessionSummary(),
    userToSystemMapping: {
      userId: null,
      authorizedSystems: []
    }
  });
}

function sessionContract() {
  return Object.assign({}, baseEnvelope("/api/session", "GET"), {
    session: currentSessionSummary(),
    note: "Session state reports authentication only. It does not imply access to any System."
  });
}

function systemMappingContract() {
  return Object.assign({}, baseEnvelope("/api/session/systems", "GET"), {
    session: currentSessionSummary(),
    userToSystemMapping: mockSession.isAuthenticated ? demoSystemAuthorization : {
      userId: null,
      authorizedSystems: []
    },
    note: "The authenticated user and authorized System are separate contract objects."
  });
}

const contracts = {
  me: Object.assign({}, baseEnvelope("/api/me", "GET"), {
    authRequired: true,
    session: currentSessionSummary(),
    response: {
      userId: "mock-user-001",
      username: "demo.user",
      displayName: "Demo User",
      selectedSystemId: "mock-system-001"
    }
  }),
  system: Object.assign({}, baseEnvelope("/api/systems/{systemId}", "GET"), {
    authRequired: true,
    systemAuthorizationRequired: true,
    response: {
      systemId: "mock-system-001",
      displayName: "Demo System",
      sourceSystem: "APPARYLLIS",
      importedSystemCount: 1
    }
  }),
  members: Object.assign({}, baseEnvelope("/api/systems/{systemId}/members", "GET"), {
    authRequired: true,
    systemAuthorizationRequired: true,
    response: {
      count: 49,
      items: [
        {
          memberId: "mock-member-001",
          displayName: "Member 001",
          privacyBucket: "public"
        }
      ]
    }
  }),
  frontHistory: Object.assign({}, baseEnvelope("/api/systems/{systemId}/front-history", "GET"), {
    authRequired: true,
    systemAuthorizationRequired: true,
    response: {
      count: 886,
      items: [
        {
          frontHistoryId: "mock-front-history-001",
          memberId: "mock-member-001",
          startedAtUtc: "2026-01-01T00:00:00Z",
          endedAtUtc: "2026-01-01T01:00:00Z"
        }
      ]
    }
  }),
  privacyBuckets: Object.assign({}, baseEnvelope("/api/systems/{systemId}/privacy-buckets", "GET"), {
    authRequired: true,
    systemAuthorizationRequired: true,
    response: {
      count: 2,
      items: [
        {
          privacyBucketId: "mock-privacy-bucket-public",
          name: "public"
        },
        {
          privacyBucketId: "mock-privacy-bucket-private",
          name: "private"
        }
      ]
    }
  }),
  customFields: Object.assign({}, baseEnvelope("/api/systems/{systemId}/custom-fields", "GET"), {
    authRequired: true,
    systemAuthorizationRequired: true,
    response: {
      count: 7,
      items: [
        {
          customFieldId: "mock-custom-field-001",
          name: "example field",
          fieldType: "text"
        }
      ]
    }
  }),
  importMetadata: Object.assign({}, baseEnvelope("/api/systems/{systemId}/import-metadata", "GET"), {
    authRequired: true,
    systemAuthorizationRequired: true,
    response: {
      importBatchCount: 1,
      sourceRecordCount: 945,
      sourceIdMapCount: 945,
      sourceSystemCount: 1
    }
  })
};

const output = document.getElementById("appOutput");
const contractButtons = document.querySelectorAll("[data-contract]");
const sessionButtons = document.querySelectorAll("[data-session-action]");
const loginForm = document.getElementById("loginForm");
const usernameInput = document.getElementById("username");
const sessionStatus = document.getElementById("sessionStatus");

function updateSessionStatus() {
  if (mockSession.isAuthenticated) {
    sessionStatus.textContent = "Session: signed in as " + mockSession.user.username + ", selected System " + mockSession.selectedSystemId;
  } else {
    sessionStatus.textContent = "Session: signed out";
  }
}

function renderPayload(payload) {
  output.textContent = JSON.stringify(payload, null, 2);
  updateSessionStatus();
}

function renderContract(key) {
  contractButtons.forEach(function(button) {
    button.setAttribute("aria-pressed", String(button.dataset.contract === key));
  });
  sessionButtons.forEach(function(button) {
    button.setAttribute("aria-pressed", "false");
  });
  renderPayload(Object.assign({}, contracts[key], {
    session: currentSessionSummary(),
    userToSystemMapping: systemMappingContract().userToSystemMapping
  }));
}

function renderSessionAction(action) {
  contractButtons.forEach(function(button) {
    button.setAttribute("aria-pressed", "false");
  });
  sessionButtons.forEach(function(button) {
    button.setAttribute("aria-pressed", String(button.dataset.sessionAction === action));
  });

  if (action === "login") {
    renderPayload(loginSuccess(usernameInput.value.trim()));
    return;
  }

  if (action === "loginFailure") {
    renderPayload(loginFailure());
    return;
  }

  if (action === "session") {
    renderPayload(sessionContract());
    return;
  }

  if (action === "systemMapping") {
    renderPayload(systemMappingContract());
    return;
  }

  if (action === "logout") {
    renderPayload(logout());
  }
}

loginForm.addEventListener("submit", function(event) {
  event.preventDefault();
  renderSessionAction("login");
});

sessionButtons.forEach(function(button) {
  if (button.dataset.sessionAction !== "login") {
    button.addEventListener("click", function() {
      renderSessionAction(button.dataset.sessionAction);
    });
  }
});

contractButtons.forEach(function(button) {
  button.addEventListener("click", function() {
    renderContract(button.dataset.contract);
  });
});

updateSessionStatus();
