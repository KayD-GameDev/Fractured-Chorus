#target photoshop

(function () {
    var CHANNEL_NAME = "Pilot_F2_Edit";
    var EXPECTED_W = 864;
    var EXPECTED_H = 1152;
    var OUTPUT_BASENAME = "ren_combat_idle_fbf_pilot_mask_f02.png";
    var REQUIRED_TAIL = "documentation/art_pipeline/review/pilot/ren_combatidle";
    var SOT_NAME_FRAGMENT = "ren_combat_battle_idle_semichibi_v2_full";

    function normalizePath(fsPath) {
        return fsPath.replace(/\\/g, "/").replace(/\/+/g, "/").replace(/\/$/, "").toLowerCase();
    }

    function isAllowedOutputFile(outputFile) {
        var norm = normalizePath(outputFile.fsName);
        if (norm.indexOf(REQUIRED_TAIL) === -1) {
            return false;
        }
        var tailIdx = norm.indexOf(REQUIRED_TAIL);
        var after = norm.substring(tailIdx + REQUIRED_TAIL.length);
        if (after.length > 0 && after.charAt(0) !== "/") {
            return false;
        }
        if (norm.indexOf("..") !== -1) {
            return false;
        }
        if (norm.indexOf(SOT_NAME_FRAGMENT) !== -1) {
            return false;
        }
        return true;
    }

    function fail(message) {
        alert(message);
        throw new Error(message);
    }

    function c2t(s) {
        return app.charIDToTypeID(s);
    }

    if (!app.documents.length) {
        fail("Không có document đang mở. Mở PSD làm việc rồi chạy lại.");
    }

    var doc = app.activeDocument;
    var w = Math.round(doc.width.as("px"));
    var h = Math.round(doc.height.as("px"));
    if (w !== EXPECTED_W || h !== EXPECTED_H) {
        fail(
            "Canvas phải là " +
                EXPECTED_W +
                "×" +
                EXPECTED_H +
                " px. Hiện tại: " +
                w +
                "×" +
                h +
                "."
        );
    }

  var channel;
    try {
        channel = doc.channels.getByName(CHANNEL_NAME);
    } catch (e) {
        fail('Không tìm thấy channel "' + CHANNEL_NAME + '".');
    }

    var scriptFile = new File($.fileName);
    var pilotDir = scriptFile.parent.parent;
    var outputFile = new File(pilotDir.fsName + "/" + OUTPUT_BASENAME);

    if (!isAllowedOutputFile(outputFile)) {
        fail(
            "Đường dẫn output không hợp lệ. Chỉ được ghi trong Review/Pilot/Ren_CombatIdle/ và không được trùng tên SoT."
        );
    }

    var savedActiveChannels = doc.activeChannels;
    var savedActiveLayer = doc.activeLayer;

    var exportDoc = null;
    try {
        doc.activeChannels = [channel];

        var desc = new ActionDescriptor();
        var ref = new ActionReference();
        ref.putEnumerated(c2t("Chnl"), c2t("Chnl"), c2t("Trgt"));
        desc.putReference(c2t("null"), ref);
        executeAction(c2t("Dplc"), desc, DialogModes.NO);

        exportDoc = app.activeDocument;

        var ew = Math.round(exportDoc.width.as("px"));
        var eh = Math.round(exportDoc.height.as("px"));
        if (ew !== EXPECTED_W || eh !== EXPECTED_H) {
            fail("Document export sai kích thước: " + ew + "×" + eh + ".");
        }

        var pngOpts = new PNGSaveOptions();
        pngOpts.compression = 6;
        pngOpts.interlaced = false;
        exportDoc.saveAs(outputFile, pngOpts, true, Extension.LOWERCASE);
    } finally {
        if (exportDoc !== null && exportDoc !== doc) {
            exportDoc.close(SaveOptions.DONOTSAVECHANGES);
        }
        try {
            app.activeDocument = doc;
        } catch (restoreDocErr) {
        }
        try {
            doc.activeChannels = savedActiveChannels;
        } catch (restoreChErr) {
        }
        try {
            doc.activeLayer = savedActiveLayer;
        } catch (restoreLayerErr) {
        }
    }

    alert(
        "Đã xuất channel \"" +
            CHANNEL_NAME +
            "\" (chỉ đọc, không sửa PSD).\n" +
            normalizePath(outputFile.fsName)
    );
})();
