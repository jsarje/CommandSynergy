window.CommandSynergy ??= {};

window.CommandSynergy.localStorage = {
    containsKey(key) {
        return localStorage.getItem(key) !== null;
    },

    async setItem(key, streamReference) {
        const arrayBuffer = await streamReference.arrayBuffer();
        const stringValue = new TextDecoder().decode(arrayBuffer);
        localStorage.setItem(key, stringValue);
    },

    getItem(key) {
        const value = localStorage.getItem(key) ?? "";
        return new TextEncoder().encode(value);
    },

    removeItem(key) {
        localStorage.removeItem(key);
    }
};