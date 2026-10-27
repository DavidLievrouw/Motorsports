const { test } = require("node:test");
const assert = require("node:assert/strict");
const { readFileSync } = require("node:fs");
const { resolve } = require("node:path");
const vm = require("node:vm");

const source = readFileSync(resolve(__dirname,
    "..", "..", "Motorsports.Web", "Components", "Layout", "ReconnectModal.razor.js"), "utf8");

function harness() {
    const listeners = new Map();
    const timers = new Map();
    const elements = new Map();
    const calls = { reconnect: 0, resume: 0, reload: 0, warnings: 0 };
    const target = () => ({
        addEventListener(name, callback) { listeners.set(name, callback); },
    });
    const modal = {
        ...target(), open: false,
        showModal() { this.open = true; },
        close() { this.open = false; },
    };
    elements.set("components-reconnect-modal", modal);
    for (const id of ["components-reconnect-button", "components-resume-button"]) {
        elements.set(id, { addEventListener() {}, disabled: false });
    }
    const document = {
        ...target(), visibilityState: "visible",
        getElementById(id) { return elements.get(id); },
    };
    const navigator = { onLine: true };
    const blazor = {
        async reconnect() { calls.reconnect++; return true; },
        async resumeCircuit() { calls.resume++; return false; },
    };
    vm.runInNewContext(source, {
        document, navigator, window: target(), Blazor: blazor,
        location: { reload() { calls.reload++; } },
        console: { warn() { calls.warnings++; } },
        setTimeout(callback, delay) {
            assert.equal(delay, 5000);
            const id = Symbol();
            timers.set(id, callback);
            return id;
        },
        clearTimeout(id) { timers.delete(id); },
    });
    return {
        document, navigator, blazor, modal, calls, timers,
        dispatch(name, event) { return listeners.get(name)(event); },
        state(state) { listeners.get("components-reconnect-state-changed")({ detail: { state } }); },
    };
}

const flush = () => new Promise(resolve => setImmediate(resolve));

test("already-visible failed page reconnects without a visibility change or reload", async () => {
    const h = harness();
    h.state("show");
    assert.equal(h.calls.reconnect, 0);
    assert.equal(h.modal.open, true);
    h.state("failed");
    await flush();
    assert.equal(h.calls.reconnect, 1);
    assert.equal(h.calls.reload, 0);
    assert.equal(h.modal.open, false);
    assert.equal(h.timers.size, 0);
});

test("outage retries repeatedly, then recovers without refreshing", async () => {
    const h = harness();
    h.blazor.reconnect = async () => {
        h.calls.reconnect++;
        if (h.calls.reconnect < 3) throw new Error("offline");
        return true;
    };
    h.state("failed");
    await flush();
    for (let i = 0; i < 2; i++) {
        assert.equal(h.timers.size, 1);
        const [id, callback] = h.timers.entries().next().value;
        h.timers.delete(id);
        callback();
        await flush();
    }
    assert.equal(h.calls.reconnect, 3);
    assert.equal(h.calls.warnings, 2);
    assert.equal(h.calls.reload, 0);
    assert.equal(h.modal.open, false);
    assert.equal(h.timers.size, 0);
});

test("hidden/offline pages wait; online and foreground events recover immediately", async () => {
    const h = harness();
    h.document.visibilityState = "hidden";
    h.state("failed");
    assert.equal(h.calls.reconnect, 0);
    assert.equal(h.timers.size, 0);
    h.document.visibilityState = "visible";
    h.navigator.onLine = false;
    h.dispatch("visibilitychange");
    assert.equal(h.calls.reconnect, 0);
    h.navigator.onLine = true;
    h.dispatch("online");
    await flush();
    assert.equal(h.calls.reconnect, 1);
});

test("expired circuit attempts resume before automatically loading a fresh page", async () => {
    const h = harness();
    h.blazor.reconnect = async () => { h.calls.reconnect++; return false; };
    h.state("failed");
    await flush();
    assert.equal(h.calls.resume, 1);
    assert.equal(h.calls.reload, 1);
    assert.equal(h.timers.size, 0);
});

test("server rejection can resume a persisted circuit without refreshing", async () => {
    const h = harness();
    h.blazor.resumeCircuit = async () => { h.calls.resume++; return true; };
    h.state("rejected");
    await flush();
    assert.equal(h.calls.reconnect, 0);
    assert.equal(h.calls.resume, 1);
    assert.equal(h.calls.reload, 0);
    assert.equal(h.modal.open, false);
});

test("foreground and page restore events never start concurrent recovery", async () => {
    const h = harness();
    let finish;
    h.blazor.reconnect = () => {
        h.calls.reconnect++;
        return new Promise(resolve => { finish = resolve; });
    };
    h.state("failed");
    h.dispatch("visibilitychange");
    h.dispatch("online");
    h.dispatch("pageshow");
    assert.equal(h.calls.reconnect, 1);
    finish(true);
    await flush();
    assert.equal(h.modal.open, false);
});

test("paused circuit resumes on foreground and successful framework recovery cancels retries", async () => {
    const h = harness();
    h.document.visibilityState = "hidden";
    h.blazor.resumeCircuit = async () => { h.calls.resume++; return true; };
    h.state("paused");
    h.document.visibilityState = "visible";
    h.dispatch("pageshow");
    await flush();
    assert.equal(h.calls.resume, 1);
    assert.equal(h.calls.reload, 0);
    h.blazor.reconnect = async () => { throw new Error("unavailable"); };
    h.state("failed");
    await flush();
    assert.equal(h.timers.size, 1);
    h.state("hide");
    assert.equal(h.timers.size, 0);
    assert.equal(h.modal.open, false);
});
