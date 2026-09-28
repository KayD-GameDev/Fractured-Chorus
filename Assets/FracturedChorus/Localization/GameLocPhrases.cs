using System.Collections.Generic;

namespace FracturedChorus.Localization
{
    public static class GameLocPhrases
    {
        private readonly struct Pair
        {
            public readonly string En;
            public readonly string Vi;

            public Pair(string en, string vi)
            {
                En = en;
                Vi = vi;
            }
        }

        private static readonly Dictionary<string, Pair> Keys = new Dictionary<string, Pair>
        {
            { "settings.volume", new Pair("VOLUME", "ÂM LƯỢNG") },
            { "settings.brightness", new Pair("BRIGHTNESS", "ĐỘ SÁNG") },
            { "settings.skip", new Pair("SKIP TEXT", "BỎ QUA THOẠI") },
            { "settings.difficulty", new Pair("DIFFICULTY", "ĐỘ KHÓ") },
            { "settings.language", new Pair("LANGUAGE", "NGÔN NGỮ") },
            { "settings.back", new Pair("BACK", "QUAY LẠI") },
            { "settings.on", new Pair("ON", "BẬT") },
            { "settings.off", new Pair("OFF", "TẮT") },
            { "settings.volume.info", new Pair("Adjust master volume across all scenes.", "Kéo thanh này để chỉnh âm lượng. Cả game đều nghe theo.") },
            { "settings.brightness.info", new Pair("Adjust screen brightness across all scenes.", "Kéo thanh này để chỉnh độ sáng. Cả game đều theo.") },
            { "settings.skip.on", new Pair("Allow skipping dialogue you have not read yet.", "Bật lên là bạn nhảy được cả thoại chưa đọc.") },
            { "settings.skip.off", new Pair("Only skip dialogue you have already read.", "Chỉ nhảy những thoại bạn đã đọc.") },
            { "settings.difficulty.onbeat", new Pair("Enemy −2 lv · HP/dmg ×0.85 · Notes ×1.1 · target party Lv13.", "Địch yếu hơn 2 cấp · máu và sát thương ×0.85 · Notes ×1.1 · đội khoảng cấp 13.") },
            { "settings.difficulty.cadence", new Pair("Baseline tune · party Lv15 vs boss Lv18.", "Mức gốc · đội cấp 15 đấu trùm cấp 18.") },
            { "settings.difficulty.offbeat", new Pair("Enemy +2 lv · HP ×1.15 · dmg ×1.2 · target party Lv17.", "Địch mạnh hơn 2 cấp · máu ×1.15 · sát thương ×1.2 · đội khoảng cấp 17.") },
            { "settings.difficulty.locked", new Pair("Locked to this save · {label} — {desc}", "File này đã khóa độ khó · {label} — {desc}") },
            { "settings.difficulty.newgame", new Pair("{desc} Applies to a new game.", "{desc} Có hiệu lực khi bạn chơi mới.") },
            { "settings.language.info", new Pair("English or Vietnamese for menus, combat, and dialogue.", "Menu, trận đánh và thoại sẽ đổi theo ngôn ngữ bạn chọn.") },
            { "menu.newgame.difficulty", new Pair("Difficulty: {label}\n{desc}\n\nLocked to this save. Change it in CONFIG.", "Độ khó: {label}\n{desc}\n\nĐộ khó này gắn với file lưu. Muốn đổi thì vào Cài đặt.") },
            { "menu.status.nosave", new Pair("No save data found.", "Chưa có file lưu nào.") },
            { "menu.status.starting", new Pair("Starting a new run…", "Đang mở ván mới…") },
            { "menu.status.loading", new Pair("Loading slot {slot}…", "Đang tải ô {slot}…") },
            { "saveload.corrupt", new Pair("Slot {slot}\nCorrupted\nThis file is damaged.\nYou can only delete it or overwrite it.", "Ô {slot}\nHỏng\nFile này hỏng hoặc sai chữ ký.\nBạn chỉ xóa hoặc ghi đè được thôi.") },
            { "saveload.empty", new Pair("Slot {slot}\nEmpty", "Ô {slot}\nTrống") },
            { "saveload.filled", new Pair("Slot {slot}\n{date} · {phase}\n{location}\nNotes {notes} · {difficulty}\nPlayed {time}", "Ô {slot}\n{date} · {phase}\n{location}\nNotes {notes} · {difficulty}\nĐã chơi {time}") },
            { "saveload.leave.title", new Pair("LEAVE THIS RUN?", "Thoát ván đang chơi?") },
            { "saveload.leave.body", new Pair("Loading {slot} drops whatever you haven't saved in this run.", "Tải {slot} sẽ bỏ phần bạn chưa lưu của ván này.") },
            { "saveload.overwrite.title", new Pair("OVERWRITE THIS SLOT?", "Ghi đè ô này?") },
            { "saveload.overwrite.damaged", new Pair("{slot} is damaged. Overwriting replaces it with the new data.", "{slot} đang hỏng. Ghi đè sẽ thay bằng dữ liệu mới.") },
            { "saveload.overwrite.body", new Pair("Overwrite {slot}? The old data will be gone.", "Ghi đè {slot}? Dữ liệu cũ sẽ mất.") },
            { "saveload.delete.title", new Pair("DELETE THIS SLOT?", "Xóa ô này?") },
            { "saveload.delete.body", new Pair("Delete slot {slot} for good? You can't get it back.", "Xóa hẳn ô {slot}? Không lấy lại được đâu.") },
            { "hub.morning", new Pair("A new morning.", "Một buổi sáng mới.") },
            { "hub.activity.day", new Pair("Choose an activity — Day", "Chọn một việc cho ban ngày") },
            { "hub.activity.night", new Pair("Choose an activity — Evening", "Chọn một việc cho buổi tối") },
            { "hub.return.title", new Pair("RETURN TO TITLE?", "Về màn hình chính?") },
            { "hub.return.body", new Pair("Any unsaved progress will be lost.", "Phần chưa lưu sẽ mất.") },
            { "map.select", new Pair("SELECT MAP", "CHỌN BẢN ĐỒ") },
            { "map.where", new Pair("Where should I go?", "Mình nên đi đâu?") },
            { "map.wordmark", new Pair("TOWNMAP", "BẢN ĐỒ") },
            { "map.enrollment", new Pair("HIMA — Enrollment", "HIMA — Nhập học") },
            { "room.shop", new Pair("SHOP", "CỬA HÀNG") },
            { "room.treasure", new Pair("TREASURE", "KHO BÁU") },
            { "room.camp", new Pair("CAMP", "TRẠI") },
            { "room.event", new Pair("EVENT", "SỰ KIỆN") },
            { "room.shop.hint", new Pair("Pick one item · {notes} Notes", "Chọn một món · {notes} Notes") },
            { "room.treasure.hint", new Pair("Pick one reward.", "Chọn một phần thưởng.") },
            { "room.camp.hint", new Pair("Pick one action.", "Chọn một việc.") },
            { "room.event.hint", new Pair("Pick one event.", "Chọn một sự kiện.") },
            { "room.legend", new Pair("Click a name to see the details.", "Bấm vào tên để xem chi tiết.") },
            { "combat.loading", new Pair("LOADING...", "ĐANG TẢI...") },
            { "combat.cover", new Pair("COVER", "CHẮN") },
            { "combat.phase", new Pair("PHASE", "NHỊP") },
            { "combat.pending", new Pair("PENDING", "CHỜ") },
            { "combat.perfect", new Pair("PERFECT", "HOÀN HẢO") },
            { "combat.note.purple", new Pair("PURPLE", "TÍM") },
            { "combat.note.leaf", new Pair("LEAF", "LÁ") },
            { "combat.note.red", new Pair("RED", "ĐỎ") }
        };

        private static readonly Dictionary<string, string> Phrases = new Dictionary<string, string>
        {
            { "VOLUME", "ÂM LƯỢNG" },
            { "BRIGHTNESS", "ĐỘ SÁNG" },
            { "SKIP TEXT", "BỎ QUA THOẠI" },
            { "DIFFICULT", "ĐỘ KHÓ" },
            { "DIFFICULTY", "ĐỘ KHÓ" },
            { "LANGUAGE", "NGÔN NGỮ" },
            { "BACK", "QUAY LẠI" },
            { "ON", "BẬT" },
            { "OFF", "TẮT" },
            { "NEW GAME", "CHƠI MỚI" },
            { "LOAD GAME", "TẢI GAME" },
            { "SAVE GAME", "LƯU GAME" },
            { "LOAD", "TẢI" },
            { "SAVE", "LƯU" },
            { "QUIT", "THOÁT" },
            { "CONFIG", "CÀI ĐẶT" },
            { "CLOSE", "ĐÓNG" },
            { "YES", "CÓ" },
            { "NO", "KHÔNG" },
            { "START", "BẮT ĐẦU" },
            { "CANCEL", "HỦY" },
            { "DELETE", "XÓA" },
            { "CONFIRM", "XÁC NHẬN" },
            { "OVERWRITE", "GHI ĐÈ" },
            { "SAVE SLOTS", "Ô LƯU" },
            { "SAVE SLOT INFO", "THÔNG TIN Ô LƯU" },
            { "OFF-BEAT ARCHIVE", "KHO OFF-BEAT" },
            { "ENTER THE OTHER SIDE", "BƯỚC SANG BÊN KIA" },
            { "NEXT", "TIẾP" },
            { "Next", "TIẾP" },
            { "DONE", "XONG" },
            { "Done", "XONG" },
            { "LEAVE", "RỜI ĐI" },
            { "Resonance Dive", "Lặn Cộng Hưởng" },
            { "Select a slot.", "Chọn một ô." },
            { "EMPTY", "TRỐNG" },
            { "Empty", "Trống" },
            { "CORRUPTED", "HỎNG" },
            { "SLOT", "Ô" },
            { "NOTES", "NOTES" },
            { "Morning", "Buổi sáng" },
            { "Day", "Ban ngày" },
            { "Evening", "Buổi tối" },
            { "Night", "Ban đêm" },
            { "Noon", "Buổi trưa" },
            { "Late Night", "Khuya" },
            { "LOADING...", "ĐANG TẢI..." },
            { "COVER", "CHẮN" },
            { "PHASE", "NHỊP" },
            { "PENDING", "CHỜ" },
            { "PERFECT", "HOÀN HẢO" },
            { "←→ Difficulty   ·   ESC Back", "←→ Độ khó   ·   ESC Quay lại" },
            { "Adjust master volume across all scenes.", "Kéo thanh này để chỉnh âm lượng. Cả game đều nghe theo." },
            { "Adjust screen brightness across all scenes.", "Kéo thanh này để chỉnh độ sáng. Cả game đều theo." },
            { "Allow skipping dialogue you have not read yet.", "Bật lên là bạn nhảy được cả thoại chưa đọc." },
            { "Only skip dialogue you have already read.", "Chỉ nhảy những thoại bạn đã đọc." },
            { "Enemy −2 lv · HP/dmg ×0.85 · Notes ×1.1 · target party Lv13.", "Địch yếu hơn 2 cấp · máu và sát thương ×0.85 · Notes ×1.1 · đội khoảng cấp 13." },
            { "Baseline tune · party Lv15 vs boss Lv18.", "Mức gốc · đội cấp 15 đấu trùm cấp 18." },
            { "Enemy +2 lv · HP ×1.15 · dmg ×1.2 · target party Lv17.", "Địch mạnh hơn 2 cấp · máu ×1.15 · sát thương ×1.2 · đội khoảng cấp 17." },
            { "No save data found.", "Chưa có file lưu nào." },
            { "Open MENU (bottom right) to check party stats, bonds, the calendar, and save slots.", "Mở MENU góc dưới phải để xem chỉ số đội, bond, lịch và ô lưu." },
            { "Resonance Dive can't be used during combat.", "Trong lúc combat không dùng được Resonance Dive." },
            { "Tap a map pin to use an activity slot. The morning quiz and the day's phase lock what's available.", "Bấm ghim trên bản đồ để dùng lượt hoạt động. Quiz sáng và nhịp trong ngày sẽ khóa những gì bạn làm được." },
            { "Hub basics are done. Wander the campus, then start a Cadence run when you feel ready.", "Phần cơ bản ở Hub xong rồi. Đi loanh quanh campus, rồi vào Cadence run khi bạn thấy sẵn sàng." },
            { "Pick a node you can reach. Battle and Elite start a fight; the boss gate ends the sector.", "Chọn ô bạn đi tới được. Battle và Elite mở trận; cổng boss thì khép khu này." },
            { "Lose a fight and you drop back to the nearest camp. HP carries between fights in a run.", "Thua trận thì bạn về trại gần nhất. Máu được giữ qua các trận trong cùng một run." },
            { "You can read the map now. Head for the boss once the party feels solid.", "Đi bản đồ ổn rồi. Cứ tiến tới boss khi đội hình đã vững." },
            { "Planning: drag units into FRONT, MID, or BACK, and drag skills onto the beat timeline. FRONT takes less damage. BACK hits harder.", "Planning: kéo nhân vật vào cột FRONT, MID hoặc BACK, rồi kéo skill lên timeline. FRONT đỡ đòn hơn. BACK đánh mạnh hơn." },
            { "Standing (the gray dot) shows the boss telegraph early. You can still move while Planning is open.", "Standing (chấm xám) cho bạn thấy trước đòn của boss. Cửa sổ Planning còn mở thì cứ đổi chỗ." },
            { "Press Execute to run the round. The music keeps going, and the scan jumps to the next beat. Counter the boss note on time, then close the skill window.", "Bấm Execute để chạy hiệp. Nhạc không dừng, vạch quét nhảy sang ô nhịp kế. Chặn nốt boss đúng nhịp, rồi đóng cửa sổ skill." },
            { "Short combat guide done. Keep the beat.", "Hướng dẫn combat ngắn xong rồi. Giữ nhịp nhé." },
            { "It's dangerous here. I'm Coda. I'll walk you out. Listen to each step, and don't rush.", "Chỗ này đang nguy hiểm. Mình là Coda. Mình sẽ dẫn cậu ra. Nghe từng bước, đừng nóng." },
            { "First, Formation. The party sits in six cells: BACK, MID, and FRONT.", "Trước hết là Formation. Đội hình có sáu ô: BACK, MID và FRONT." },
            { "Each spot gives a different buff, depending on the fight.", "Mỗi chỗ cho một buff khác, tùy trận đang đánh." },
            { "FRONT cuts the damage you take.", "FRONT giảm sát thương bạn phải chịu." },
            { "MID raises damage. BACK raises buffs and dodge.", "MID tăng sát thương. BACK tăng buff và né." },
            { "Look at the party and the fight, then place people where they fit.", "Nhìn đội hình và tình huống, rồi đặt mọi người vào chỗ hợp." },
            { "Move Ren into the FRONT cell on the same row.", "Kéo Ren sang ô FRONT cùng hàng." },
            { "Nice.", "Tốt lắm." },
            { "The number on a note is the note total. You have to counter the boss hit.", "Số trên nốt là tổng số nốt. Bạn phải chặn đòn của boss." },
            { "Tap the character.", "Bấm vào nhân vật." },
            { "This is the character's skill. Drag it onto the timeline to block the boss note.", "Đây là skill của nhân vật. Kéo xuống timeline để chặn nốt boss." },
            { "Each skill has two parts. The big note is when they strike. Put it under the boss note to counter and deal damage.", "Mỗi kỹ năng có hai phần. Nốt to là lúc nhân vật tung đòn. Đặt dưới nốt boss để chặn và gây sát thương." },
            { "The small note doesn't attack. It's the wait before the strike starts and ends.", "Nốt nhỏ không đánh. Đó là khoảng chờ trước khi đòn bắt đầu và kết thúc." },
            { "Small notes can't overlap. Leave room when you place them.", "Nốt nhỏ không chồng lên nhau được. Đặt thì chừa chỗ." },
            { "Now drop my skill in too.", "Giờ đặt skill của mình vào nữa nhé." },
            { "Drag Coda's skill onto the timeline.", "Kéo skill của Coda xuống timeline." },
            { "Look at the first note. The number just dropped. That is a counter: your skill ate one hit, so the boss has less left on that beat. A note nobody covers still comes down. You will feel that one.", "Nhìn nốt đầu tiên kìa. Con số vừa tụt. Đó là counter: chiêu của cậu nuốt một nhịp, nên đòn trên beat đó còn lại ít hơn. Nốt nào không ai đỡ thì vẫn rơi xuống. Lát nữa cậu sẽ thấy rõ." },
            { "One skill from Ren, one from me, on two notes. Then you can press Execute. Leave the third note. That one still hits.", "Một chiêu của Ren, một chiêu của mình, trúng hai nốt. Rồi mới bấm Execute. Cứ để nốt thứ ba. Nốt đó vẫn nện xuống." },
            { "You've got the idea.", "Tuyệt, bạn nắm cơ chế rồi." },
            { "Next is the Quick Time Event.", "Tiếp theo là Quick Time Event." },
            { "Place skills in the cells under the timeline.", "Đặt skill vào các ô dưới timeline." },
            { "Drag a skill onto the timeline.", "Kéo skill xuống timeline." },
            { "Press Execute.", "Bấm Execute." },
            { "This is a QTE. Hit Space on time and you deal extra damage.", "Đây là QTE. Bấm Space đúng lúc thì sát thương tăng thêm." },
            { "Nice work.", "Tuyệt, bạn làm tốt lắm." },
            { "Do the same for the notes left. I'll back you up.", "Làm tương tự với các nốt còn lại. Mình hỗ trợ." },
            { "The outer ring is shrinking. That's the beat timer.", "Vòng ngoài đang thu lại. Đó là vòng đếm nhịp." },
            { "The inner ring is the mark. Perfect is when the two rings meet.", "Vòng trong là mốc. Hai vòng chạm nhau là lúc Perfect." },
            { "Hit it now. Click outside the bubble for a Perfect.", "Canh đúng lúc này. Bấm chuột bên ngoài bong bóng để được Perfect." }
        };

        public static string Get(string key, GameLanguage language)
        {
            if (string.IsNullOrEmpty(key) || !Keys.TryGetValue(key, out var pair))
            {
                return key ?? string.Empty;
            }

            return language == GameLanguage.Vietnamese ? pair.Vi : pair.En;
        }

        public static bool TryPhrase(string english, out string vietnamese)
        {
            return Phrases.TryGetValue(english, out vietnamese);
        }
    }
}
