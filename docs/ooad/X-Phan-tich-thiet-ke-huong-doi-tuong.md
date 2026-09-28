# X. Phân tích và thiết kế hướng đối tượng

**Đồ án:** Phát triển game 2D *Fractured Chorus*  
**Nhóm:** Day Dreamers  
**Nguồn thiết kế:** *Fractured-Chorus-GDD.docx*, mục I–IX  
**Nguồn hiện thực:** prototype Unity trong `Assets/FracturedChorus` (tháng 9/2026)  
**Cách phân tích:** quy trình OOAD của môn, lần lượt use case, hoạt động, lớp, trạng thái, tuần tự, thành phần và triển khai

Chương này đứng trước phần kết luận. Khi ghép vào báo cáo Word, mục **Kết luận và hướng phát triển** hiện đang đánh số X cần chuyển thành **XI**.

Sơ đồ mở bằng diagrams.net (kéo thả file, hoặc File → Open):

- [`OOAD-UseCase.drawio`](OOAD-UseCase.drawio)
- [`OOAD-Activity-ChayPhase.drawio`](OOAD-Activity-ChayPhase.drawio)
- [`OOAD-Lop-Combat.drawio`](OOAD-Lop-Combat.drawio)
- [`OOAD-Lop-Meta-Run-VN.drawio`](OOAD-Lop-Meta-Run-VN.drawio)
- [`OOAD-TrangThai-CombatSession.drawio`](OOAD-TrangThai-CombatSession.drawio)
- [`OOAD-Sequence-PlanningExecute.drawio`](OOAD-Sequence-PlanningExecute.drawio)
- [`OOAD-Component-Deployment.drawio`](OOAD-Component-Deployment.drawio)

---

## X.1. Việc chương này phải trả lời

Các chương I–IX mô tả người chơi làm gì: chọn độ khó, sống một ngày ở Lumina, đi bản đồ hầm, đặt chiêu trên nhịp, xem thoại, nâng chỉ số trận và chỉ số đời sống. Những chương đó trả lời câu hỏi thiết kế game. Chương này trả lời câu hỏi phần mềm. Câu hỏi gồm ba vế. Dữ liệu nào thuộc về lớp nào. Lớp nào được phép đổi dữ liệu đó. Lớp nào chỉ được nhìn kết quả rồi vẽ lên màn hình.

Phân tích đi theo ba mô hình của bài giảng nhập môn. Mô hình đối tượng cho cấu trúc tĩnh, nên ra sơ đồ lớp. Mô hình động cho cách một đối tượng đổi trạng thái theo sự kiện, nên ra sơ đồ trạng thái của `CombatSession`. Mô hình chức năng cho luồng xử lý của một việc người chơi làm xong, nên ra sơ đồ hoạt động và sơ đồ tuần tự của một phase combat.

Thiết kế giữ đúng ranh giới đã có trong code. Prototype đã chạy vòng Planning–Execute, đã sinh map theo seed, và đã giữ lịch cùng bond trong hồ sơ meta. Sơ đồ không liệt kê từng lớp hiệu ứng hình ảnh. Những lớp đó vẽ đòn và giao diện. Chúng đọc sự kiện. Chúng không sở hữu luật chơi.

## X.2. Phạm vi bám theo bản đang chạy

Phạm vi OOAD trùng với những gì GDD đã khóa và Unity đã có lớp tương ứng. Việc GDD còn ghi là mở thì chương này không bịa thêm lớp.

| Phần trong GDD | Hành vi đã có lớp | Việc còn mở, sơ đồ không thêm lớp |
|---|---|---|
| Độ khó On Beat / Cadence / Off Beat | `DifficultyRuntime` nhân máu, sát thương, lệch level địch | Luật đặt chiêu không đổi theo mức khó |
| Combat: Planning, Execute, beat đều, Dual Grid 2×3 | `CombatSession`, `BeatTimelineEngine`, `DualGrid` | Clash sâu, Morale đầy đủ, kit đối thủ còn số tạm |
| Run map, boss ở tầng 16 | `MapGenerator`, `MapGraph`, `RunState` | Chưa phải mọi node battle và elite đều mở combat |
| Lịch, slot ngày, bond Echo Key | `CalendarState`, `BondState`, `HubPhaseDriver` | Resonance Dive và cờ vault đúng hạn còn là hướng làm sau |
| Visual novel | `VnRuntimeController` phát `VnScriptSO` | Thoại không được gọi vào lớp combat |
| Hai lớp chỉ số | Chỉ số trận trên `UnitStats`; đời sống trên `SocialStatsState` | Điểm bên này không cộng sang bên kia |

Một trận boss remix đang khóa ở 152 BPM, 677 beat, mỗi phase 22 beat, lookahead 3 nhịp, intro 12 beat. Cửa sổ planning vừa dời quân vừa gán skill. Nhạc không dừng. Planning chỉ giảm âm lượng xuống 0,7 và lọc thấp về 900 Hz. Những con số này nằm trong `CombatTimelineProfile` và `CombatMusicController`. Chúng không nằm rải trong từng chiêu.

## X.3. Tác nhân và ca sử dụng

Tác nhân là người hoặc hệ thống nằm ngoài biên phần mềm đang xét. Người chơi là tác nhân chính. Đồng hồ nhạc là tác nhân phụ: `CombatMusicController` báo beat tuyệt đối, người chơi không bấm beat đó. Lịch, bond, unit và chiêu nằm trong biên, nên chúng là lớp, không phải tác nhân.

Mỗi ca sử dụng là một việc người chơi làm xong trong một lần. Chỉ một nhóm lớp được sửa trạng thái của ca đó.

1. **Chọn độ khó rồi vào game.** `DifficultyRuntime` trả bộ hệ số cho máu địch, đòn địch và Notes. Luật đặt chiêu giống nhau ở cả ba mức.
2. **Bắt đầu một ngày ở thành phố.** `HubPhaseDriver` đọc `CalendarState`. Buổi sáng là quiz. Trong ngày có tối đa hai slot hoạt động, vì `MaxSlotsPerDay` bằng 2. Hết slot thì sang buổi kế hoặc sang ngày mới.
3. **Làm hoạt động đời sống.** `CampusHubController` gọi `SocialStatsState.AddExp`. Hạng đời sống tăng theo ngưỡng kinh nghiệm riêng của trục đó. Hạng này không ghi vào `UnitStats`.
4. **Gặp người và tăng bond.** `BondState` giữ một `BondProgress` cho mỗi NPC: Ren, Charlotte, Coda, Ryo, Mei Lin, Astra. Mỗi bond có Echo Key và trần hạng của Arc 1. Menu bond chỉ đọc hạng rồi vẽ.
5. **Vào hầm và đi node.** `MapGenerator.Generate` nhận seed và dựng `MapGraph` từ các `MapNodeData`. `RunState` nhớ node đã đi. Người chơi chỉ chọn được node kề trên đường còn mở. Tầng boss là đích của run.
6. **Vào trận.** `EncounterRuntimeFactory` dựng lưới và đơn vị. `PartyLoadoutApplicator` gắn chỉ số trận và bộ chiêu đã mở theo cấp đội. `CombatController` mở intro 12 beat, rồi mở cửa sổ planning.
7. **Suy nghĩ trong planning.** Người chơi kéo quân trên `DualGrid` và thả chiêu thành `AgendaEntry` trên `BeatTimelineEngine`. `CombatSession.TryAssignPlayerAction` từ chối nếu footprint S1, S hoặc S2 của cùng một người đã chiếm beat đó. Prep, Delay và ReduceS2 giải ngay trong cửa sổ này, trước khi bấm Execute.
8. **Chạy một phase.** `ConfirmPlanningAndExecute` đóng cửa sổ, neo timeline vào beat nhạc kế, rồi quét đủ 22 beat. Mỗi beat tới, session thực thi chiêu người chơi và đòn địch đã báo trước.
9. **Đỡ đúng lúc.** Người chơi bấm chắn. `CombatCounterResolver` so thời điểm bấm với nhịp đòn và trả kết quả đỡ. `CounterPresentationDriver` chỉ vẽ chữ và chip. Lớp vẽ không tính lại sát thương.
10. **Kết thúc trận hoặc một ngày.** Thắng hoặc thua phát sự kiện `OnEncounterEnded`. Kinh nghiệm trận cộng vào cấp đội. Notes và tiến độ run ghi vào hồ sơ meta. `GameMetaSaveLoad` ghi file. Các view không tự mở đĩa.

Ca 7 và ca 8 là lõi của trận. Sơ đồ hoạt động, sơ đồ trạng thái và sơ đồ tuần tự ở các mục sau đều bám hai ca này. Nếu trộn hai ca vào một lớp vừa vẽ nút vừa trừ máu, thì sửa luật footprint sẽ phải sửa cả nút bấm.

Sơ đồ use case nằm ở `OOAD-UseCase.drawio`. Biên hệ thống là *Fractured Chorus*. Người chơi đứng ngoài biên và nối tới mười ca trên. Đồng hồ nhạc chỉ nối tới ca **Chạy một phase**, vì chỉ ca đó đọc beat tuyệt đối. Ca **Đỡ đúng lúc** nằm trong lúc phase đang chạy, nên quan hệ với ca **Chạy một phase** là extend: đỡ chỉ xảy ra khi có đòn chạm người chơi. Ca **Suy nghĩ trong planning** include bước kiểm footprint, vì mọi lần đặt chiêu đều phải qua bước đó.

## X.4. Luồng hoạt động của một phase

Sơ đồ hoạt động mô tả các bước của ca **Chạy một phase**, kể cả đoạn planning ngay trước nó. Sơ đồ nằm ở `OOAD-Activity-ChayPhase.drawio`. Bốn cột việc thuộc về bốn bên: người chơi, `CombatController`, `CombatSession`, và đồng hồ nhạc.

Luồng bắt đầu khi intro đã xong. Session mở planning. Người chơi kéo quân và thả chiêu. Session kiểm footprint. Nhánh sai thì từ chối và người chơi đặt lại. Nhánh đúng thì timeline thêm `AgendaEntry`. Người chơi bấm Execute. Controller gọi `ConfirmPlanningAndExecute`. Nhạc vẫn chạy. Session lấy beat tuyệt đối, cộng vào mốc neo, rồi mới cho thước nhịp cục bộ tiến. Trong 22 beat, mỗi beat tới thì session giải chiêu và đòn địch tới hạn. Nếu người chơi bấm chắn đúng nhịp đòn, resolver trả kết quả đỡ rồi unit mới trừ máu hoặc trừ khiên. Hết 22 beat, controller mở planning mới. Intro không chạy lại. Nhạc không cắt.

Điểm dễ viết nhầm nằm ở nút Execute. Execute không dừng bài hát rồi tính toàn bộ sát thương một lần. Execute mở một đoạn dài 22 beat. Sát thương xảy ra tại beat mà `AgendaEntry` hoặc `EnemyTelegraph` đã ghi từ trước.

## X.5. Danh từ trong GDD và lớp giữ danh từ đó

Lớp thực thể lấy từ danh từ mà hệ thống lưu hoặc cho hành động. Chữ trên nút, sprite, màu và hạt hiệu ứng bị loại, vì chúng là cách trình bày. Đổi sprite Strike không được đổi `AgendaEntry`.

| Danh từ trong GDD | Lớp giữ trạng thái | Lý do lớp này tồn tại |
|---|---|---|
| Trận, pha, cửa sổ suy nghĩ | `CombatSession` | Một nơi biết trận đang planning, đang chạy, hay đã kết thúc |
| Nhịp, hàng chờ chiêu, báo đòn địch | `BeatTimelineEngine` | Giữ danh sách `AgendaEntry` và `EnemyTelegraph`, biết beat đang quét |
| Ô đội hình 2×3 | `DualGrid`, `GridCell` | Biết ai đứng ô nào, và mỗi phe được đứng tối đa bao nhiêu người |
| Nhân vật trong trận | `CombatUnit` | Máu hiện tại, khiên, Prep, còn sống hay không |
| Chỉ số trận | `UnitStats` | Sức, máu tối đa, tốc độ hành động; đọc từ stat block |
| Chiêu đã đặt | `AgendaEntry` | Một lần dùng chiêu của một người tại một beat |
| Hành động lúc tới beat | `ICombatAction`, `SkillActionCommand` | Lệnh thực thi, tách khỏi dữ liệu trên timeline |
| Độ khó | `DifficultyRuntime` | Bộ hệ số dùng chung; không lưu máu từng quái |
| Bản đồ một lần đi hầm | `MapGraph`, `MapNodeData` | Đồ thị tầng và cột, loại node, đã đi chưa |
| Lượt đi trên map | `RunState` | Seed, node hiện tại, các node đã thăm |
| Ngày và buổi | `CalendarState`, `GameDate` | Ngày Arc 1, buổi, slot đã dùng, quiz sáng đã xong chưa |
| Gắn kết | `BondState`, `BondProgress` | Hạng và kinh nghiệm bond theo từng NPC |
| Chỉ số đời sống | `SocialStatsState` | Kinh nghiệm và hạng từng trục xã hội |
| Thoại | `VnScriptSO`, `VnBeat` | Kịch bản; `VnRuntimeController` chỉ phát từng beat |
| Hồ sơ giữa các scene | `GameMetaSaveData` | Gói lịch, bond, xã hội và cấp đội để ghi và đọc |

Ba nhóm trách nhiệm tách khỏi bảng trên. Nhóm biên nhận thao tác và vẽ kết quả: `CombatController`, `BeatTimelineUIView`, `BoardDragController`, `CampusHubController`, `VnRuntimeController`. Nhóm này không tự cộng Prep và không tự sinh node. Nhóm điều khiển xếp thứ tự việc: `CombatSession` cho một trận, `HubPhaseDriver` cho một ngày, `RunMapController` cho một lần chọn node, `MapGenerator` chỉ lúc tạo map. Nhóm thực thể giữ sự thật và sửa trường của chính nó. `CombatUnit.GainPrep` nằm trên unit vì Prep là của người đó. `RunState.CanTravelTo` nằm trên tiến độ run vì luật “đã thăm và được đi tiếp” là của đường đi, không phải của hình node.

Không có thao tác nào trên `SocialStatsState` ghi vào `UnitStats`. GDD đòi hai câu hỏi tách nhau: đội có chịu nổi đòn trong trận hay không, và Ren có gặp được người cần gặp ở thành phố hay không. Hai lớp là cách giữ hai câu hỏi đó trong code.

## X.6. Sơ đồ lớp combat

Tệp `OOAD-Lop-Combat.drawio`. Kim cương đặc nghĩa là cấu thành: phần mất khi toàn thể mất. Kim cương rỗng nghĩa là kết tập: toàn thể giữ danh sách, phần vẫn sống chỗ khác. Nét đứt là phụ thuộc tạm. Tam giác rỗng nét đứt là hiện thực giao diện.

`CombatController` biết `CombatSession` và biết `ICombatMusicSync`. Controller không giữ công thức sát thương. Nó gọi `ConfirmPlanningAndExecute` và `StartRound`, rồi nhận lúc một đoạn 22 beat chạy xong.

`CombatSession` cấu thành `BeatTimelineEngine`, `DualGrid` và `CoverRuntime`. Ba phần này sinh cùng trận và mất khi trận hủy. Session phụ thuộc `DifficultyRuntime` lúc dựng địch và lúc tính thưởng. Hệ số độ khó là bảng tra dùng chung, không phải một cục máu của riêng trận này.

`DualGrid` cấu thành các `CombatUnit` đang đứng trên sân. Tối đa 4 người chơi và 6 địch. Mỗi phe là lưới 2 hàng và 3 cột. `CombatUnit` cấu thành một `UnitStats`. Máu hiện tại nằm trên unit. Máu tối đa và tốc độ nằm trên stats.

`BeatTimelineEngine` kết tập nhiều `AgendaEntry` và nhiều `EnemyTelegraph`. Một entry vẫn trỏ tới `CombatUnit` và `SkillDefinitionSO`. Xóa một chiêu khỏi hàng chờ không xóa nhân vật.

`SkillActionCommand` hiện thực `ICombatAction`. Giao diện có `Delay`, `CanExecute` và `Execute`. Command là lệnh chạy khi beat tới. `AgendaEntry` là chỗ đặt lệnh trên thước nhịp. Tách hai thứ này để dời một nốt, tức là đổi `BeatIndex`, mà không phải sửa cách chiêu trừ máu.

`EncounterRuntimeFactory` tạo encounter rồi giao cho bootstrap. Sau bước tạo, factory không quét beat. `CombatMusicController` hiện thực `ICombatMusicSync`. Nhạc là đồng hồ tuyệt đối. Timeline là thước cục bộ của trận. Khi người chơi bấm Execute, session neo thước cục bộ vào beat nhạc kế tiếp. Trễ tay vì vậy chỉ lệch khoảng một beat, khoảng 0,39 giây ở 152 BPM. Bài hát không dừng để chờ.

`CoverRuntime` thuộc session vì thước Cover là luật của trận: ô trống tích vào gauge, rồi kích hoạt trong planning. Luật đó không thuộc một sprite khiên.

## X.7. Sơ đồ lớp thành phố, hầm, thoại và hồ sơ

Tệp `OOAD-Lop-Meta-Run-VN.drawio`.

`CampusHubController` giữ `HubPhaseDriver`. Driver đọc `CalendarState` của hồ sơ đang chơi. `CalendarState` giữ `GameDate`, buổi trong ngày, số slot đã dùng và cờ quiz sáng. `ConsumeActivitySlot` trừ một slot. `AdvanceDay` sang ngày kế. `IsArcComplete` đúng khi ngày vượt quá ngày cuối Arc 1. Hạn vault đọc bằng `DaysUntilVaultDeadline`. Những phép này nằm trên lịch vì chúng là luật ngày, không phải luật của nút trên bản đồ.

`BondState` cấu thành nhiều `BondProgress`. Mỗi tiến độ có NPC, Echo Key, hạng, kinh nghiệm và trần Arc. `SocialStatsState` đứng cạnh bond và không kế thừa bond. Bond là quan hệ với một người. Chỉ số đời sống là hạng của Ren trên các trục hoạt động. GDD tách hai thứ này, nên sơ đồ giữ một thực thể cho mỗi thứ.

`RunMapController` dùng `RunState` và `MapGraph`. `MapGenerator` tạo graph rồi rút lui, không nhớ node hiện tại. `MapGraph` cấu thành các `MapNodeData`. Node biết tầng, cột, loại, có phải boss không, đã xóa chưa, và danh sách id node đi tới. `RunState.EnterNode` chỉ thành công khi `CanTravelTo` đồng ý. View vì vậy có thể vẽ mọi node, còn luật đi đường nằm một chỗ.

`RunMapSceneLoader` chuyển từ node boss sang scene combat. Loader không sinh lại graph. Graph và `RunState` còn sống trong tiến độ run, nên lúc quay ra map vẫn đúng seed và đúng node đã thăm.

`VnRuntimeController` phát một `VnScriptSO`. Script là dữ liệu thoại. Controller phát beat, nhận lựa chọn, rồi báo `Finished`. Thoại mở đầu và sự kiện thành phố dùng cùng kiểu chạy này. Chúng không gọi `CombatSession`.

`GameMetaSaveLoad` đọc và ghi `GameMetaSaveData`. Gói này gom lịch, bond, chỉ số đời sống và phần meta cần mang qua scene. Các màn hình không mở file riêng. Một đường ghi giúp bond vừa tăng trong thoại vẫn còn khi người chơi vào hầm.

`PartyLoadoutApplicator` là cầu một chiều từ meta sang combat. Nó đọc cấp đội cùng bộ chiêu đã mở, rồi áp vào unit lúc vào trận. Trận không ghi ngược hạng đời sống.

## X.8. Trạng thái của một trận

Sơ đồ trạng thái của lớp `CombatSession` nằm ở `OOAD-TrangThai-CombatSession.drawio`. Bốn trạng thái khớp cờ đang chạy trong code.

**Intro.** `IsCombatIntroActive` đúng. Thước nhịp chạy để người chơi nhìn, và 12 beat intro không tính vào phase. Hết intro thì sang Planning.

**Planning.** `IsPlanningWindowOpen` đúng khi phase là Planning, timeline không chạy, intro đã tắt, và trận chưa kết thúc. Ở trạng thái này người chơi đổi ô và gán chiêu. Nhạc vẫn phát, nhưng bị giảm âm và lọc thấp.

**Execute.** Timeline chạy một đoạn 22 beat. Session giải chiêu và đòn theo beat. Người chơi không đặt chiêu mới trong đoạn này. Đỡ đòn vẫn xảy ra khi có đòn chạm.

**Kết thúc trận.** `IsEncounterEnded` đúng khi một phe không còn ai sống, hoặc trận bị kết thúc bởi luật encounter. Từ Planning và từ Execute đều có thể sang trạng thái này. Không có đường quay lại Intro trong cùng một trận.

Hết Execute mà trận chưa kết thúc thì quay về Planning. Đoạn planning sau không phát lại intro. Trạng thái ngày ở thành phố không nằm trên sơ đồ này. Ngày thuộc `CalendarState`, có buổi sáng, slot trong ngày, và ngày kế. Trộn ngày vào trạng thái trận sẽ khiến một cờ “đang planning” nuốt luôn ý “đã qua buổi sáng”.

## X.9. Tuần tự từ lúc đặt chiêu tới hết phase

Sơ đồ tuần tự nằm ở `OOAD-Sequence-PlanningExecute.drawio`. Các đối tượng trên lược đồ là `:Người chơi`, `:CombatController`, `:CombatSession`, `:BeatTimelineEngine`, `:CombatUnit` và `:ICombatMusicSync`. Thời gian đi từ trên xuống. Các phép gọi dưới đây là phép đã có trên lớp, và là phép cần giữ trên sơ đồ lớp.

1. Controller kết thúc intro và gọi session mở planning.
2. Người chơi kéo quân. Controller hỏi session. `DualGrid` đổi ô. Unit không tự tìm ô hàng xóm.
3. Người chơi thả chiêu. Session gọi `TryAssignPlayerAction`. Nếu beat và footprint hợp lệ, timeline thêm một `AgendaEntry`. Nếu không hợp lệ, session trả về từ chối và hàng chờ không đổi.
4. Hiệu ứng planning ghi trên entry và trên unit ngay trong cửa sổ này. Delay đẩy nốt. ReduceS2 giảm độ dài S2. Prep cộng vào unit bằng `GainPrep`.
5. Người chơi bấm Execute. Controller gọi `ConfirmPlanningAndExecute`. Session hỏi đồng hồ nhạc beat tuyệt đối hiện tại, tính beat neo kế tiếp, rồi cho timeline chạy.
6. Đồng hồ báo beat. Timeline tiến scan cục bộ. Mỗi beat, session giải chiêu trong agenda và telegraph địch tới hạn. `SkillActionCommand.Execute` chạy trên `CombatUnit`.
7. Đòn chạm người chơi đi qua `CombatCounterResolver`. Unit trừ máu hoặc trừ khiên. Session phát `OnUnitHpChanged`. View đổi thanh máu sau sự kiện đó, không đổi trước.
8. Đủ 22 beat, controller nhận hết đoạn và gọi mở planning mới.

Bước 5 và bước 6 cho thấy vì sao nhạc và timeline là hai đối tượng. Nhạc đếm beat của bài hát từ lúc vào trận. Timeline đếm beat của đoạn đang chơi. Neo ở bước 5 là phép nối hai thước đó. Thiếu phép neo thì thước cục bộ sẽ nhảy theo mọi nhịp nhạc đã trôi trong lúc người chơi còn đang suy nghĩ.

Sau lược đồ này, sơ đồ lớp combat giữ các phép `TryAssignPlayerAction`, `ConfirmPlanningAndExecute`, `GainPrep`, `Execute` và sự kiện `OnUnitHpChanged`. Đó là bước cập nhật lớp sau khi đã vẽ tuần tự.

## X.10. Mẫu đang dùng, và chỗ mẫu dừng

Bốn mẫu dưới đây là mẫu có mặt trong code, trùng tên với bài mẫu thiết kế. Mỗi mẫu gắn với một chỗ cụ thể, không phải một nhãn dán chung cho cả game.

**Command.** `ICombatAction` là giao diện lệnh. `SkillActionCommand` là lệnh cụ thể. `BeatTimelineEngine` giữ thời điểm. Lệnh giữ việc sẽ xảy ra lúc beat tới. Thêm một loại hành động sau này là thêm một lớp hiện thực giao diện. Vòng quét beat không phải sửa.

**Observer.** `CombatSession` và `BeatTimelineEngine` phát sự kiện: đổi phase, gán chiêu, quét tới một beat, đổi máu, hết trận, đòn địch đã giải, chiêu người chơi đã giải. View đăng ký rồi vẽ. Luật không đọc vị trí pixel của thanh máu.

**Factory.** `EncounterRuntimeFactory` tạo lưới, unit và encounter lúc vào trận. `MapGenerator` tạo `MapGraph` lúc bắt đầu run. Sau bước tạo, factory không giữ máu và không giữ node hiện tại. Đây là factory tạo cụm đối tượng đúng hình, không phải Factory Method kiểu `Creator` trả về `Product` trong bài lab cơ sở dữ liệu.

**Tách model, view và controller.** Session, timeline, lưới và unit là model. Chúng không vẽ. `CombatController` là controller của trận: nhận Execute, gọi session, nhận lúc hết đoạn. Các view là phần nhìn. Cách này khớp quy ước dự án: luật nằm trong C#, scene giữ bố cục, Play Mode gắn dữ liệu vào vật đã có trên Hierarchy.

**Singleton không dùng cho trận.** Một trận không phải một đối tượng duy nhất của cả ứng dụng. Hồ sơ meta sống lâu hơn trận, run sống lâu hơn một node, thoại sống trong một cảnh. Nếu nhét lịch, beat và thoại vào một `GameManager` kiểu singleton, thì lỗi một vòng sẽ không chỉ ra được dữ liệu hỏng thuộc ngày, thuộc run, hay thuộc trận.

## X.11. Thành phần phần mềm và chỗ chúng chạy

Sơ đồ thành phần và sơ đồ triển khai nằm chung tệp `OOAD-Component-Deployment.drawio`, mỗi sơ đồ một trang.

Năm thành phần đóng gói theo thư mục code, và theo thời gian sống khác nhau.

| Thành phần | Việc nó sở hữu | Phụ thuộc |
|---|---|---|
| Combat | Trận, timeline, lưới, lệnh chiêu, độ khó | Meta để lấy trang bị lúc vào trận; Audio để lấy beat |
| RunMap | Sinh map, tiến độ đi node, mở scene boss | Meta để biết run còn sống; Combat khi vào trận |
| Hub | Ngày, buổi, hoạt động, menu bond | Meta để đọc và sửa lịch, bond, chỉ số đời sống |
| Narrative | Phát kịch bản thoại | Meta khi một lựa chọn cộng bond hoặc cờ truyện |
| Meta | Hồ sơ: lịch, bond, xã hội, cấp đội, ghi file | Không phụ thuộc Combat, Hub hay Narrative |
| Audio | Đồng hồ nhạc combat | Không phụ thuộc luật trận |

Mũi tên phụ thuộc chỉ ra chiều “cần đến”. Combat cần Meta lúc vào trận, qua `PartyLoadoutApplicator`. Meta không cần Combat. Narrative không cần Combat. Hai chiều cấm này giữ cho một cảnh thoại mở được khi không có trận nào trong bộ nhớ.

Triển khai của prototype là một tiến trình Unity trên máy người chơi. Các scene (`CombatPrototype`, run map, hub, visual novel) chạy trong tiến trình đó, không phải trên các máy riêng. `GameMetaSaveData` ghi xuống đĩa của cùng máy đó qua `GameMetaSaveLoad`. Nhạc boss phát từ `AudioSource` trong scene combat. Không có nút máy chủ trong lát cắt hiện tại. Vì vậy sơ đồ triển khai có một node *Máy người chơi*, bên trong là tiến trình Unity, và một node *Đĩa cục bộ* nối tới tiến trình bằng liên kết đọc-ghi hồ sơ.

## X.12. Điều chương này cố ý không làm

Sơ đồ không thêm lớp cho Clash–Break đầy đủ, Morale, hay bộ kit số cứng của Astra, Mimi và Kiki. GDD còn ghi các mục đó là việc khóa sau. Thêm lớp khi luật chưa khóa sẽ tạo thực thể không có trách nhiệm thật.

Sơ đồ cũng không vẽ từng choreographer hiệu ứng. Chúng đọc báo đòn đã giải, phát hình, rồi kết thúc. Chúng không được sửa `AgendaEntry` sau khi beat đã resolve.

Khi ghép chương vào báo cáo, mục kết luận giữ nguyên nội dung hiện có và đổi số thành XI. Phần hướng phát triển của mục đó vẫn đúng: khóa kit boss Arc 1, siết cờ lịch với hầm, và chỉ lúc đó mới thêm lớp mới vào các sơ đồ này.

## X.13. Kế hoạch yêu cầu chức năng (FR)

Yêu cầu chức năng ở đây là việc người chơi làm xong và hệ thống phải để lại một kết quả kiểm được. Mỗi mã FR gắn với một ca sử dụng ở mục X.3 và với đúng lớp được phép sửa trạng thái. Lớp vẽ không đứng trong cột “lớp giữ kết quả”.

Kế hoạch chia hai nhóm. Nhóm FR-01 đến FR-10 là việc bản Unity tháng 9/2026 đã chạy. Nhóm này là hợp đồng giữ nguyên: sửa combat hoặc hub mà làm lệch kết quả của một mã thì bản đó chưa đạt. Nhóm FR-11 đến FR-13 là việc GDD đã nêu nhưng luật chi tiết chưa khóa. Các mã này có hướng làm, chưa có lớp mới.

| Mã | Việc người chơi làm | Kết quả phải còn sau khi xong | Lớp giữ kết quả | Ca sử dụng |
|---|---|---|---|---|
| FR-01 | Chọn On Beat, Cadence hoặc Off Beat rồi vào game | Máu địch, đòn địch và Notes đổi theo hệ số mức đó. Luật đặt chiêu không đổi | `DifficultyRuntime` | 1 |
| FR-02 | Bắt đầu một ngày ở thành phố | Buổi sáng là quiz. Trong ngày còn tối đa hai slot. Hết slot thì sang buổi kế hoặc sang ngày mới | `CalendarState`, `HubPhaseDriver` | 2 |
| FR-03 | Làm một hoạt động đời sống | Kinh nghiệm và hạng của trục xã hội tăng. `UnitStats` không đổi | `SocialStatsState` | 3 |
| FR-04 | Gặp một NPC và tăng bond | Một `BondProgress` của NPC đó tăng hạng hoặc kinh nghiệm, trong trần Arc 1 | `BondState` | 4 |
| FR-05 | Chọn một node trên map hầm | Chỉ node kề, trên đường còn mở, mới được vào. Seed và các node đã thăm còn nguyên | `RunState`, `MapGraph` | 5 |
| FR-06 | Vào một trận từ node | Lưới 2×3, tối đa 4 người chơi và 6 địch, chỉ số trận và chiêu đã mở theo cấp đội | `EncounterRuntimeFactory`, `PartyLoadoutApplicator` | 6 |
| FR-07 | Đặt chiêu trong planning | Một `AgendaEntry` trên beat còn trống. Cùng một người không chiếm hai lần footprint S1, S hoặc S2 trên cùng beat | `CombatSession`, `BeatTimelineEngine` | 7 |
| FR-08 | Bấm Execute | Nhạc không dừng. Timeline neo vào beat kế và quét đúng 22 beat. Sát thương rơi tại beat đã ghi | `CombatSession`, `ICombatMusicSync` | 8 |
| FR-09 | Bấm chắn khi đòn chạm | `CombatCounterResolver` trả kết quả đỡ. Máu hoặc khiên trừ sau kết quả đó. Lớp vẽ không tính lại | `CombatCounterResolver`, `CombatUnit` | 9 |
| FR-10 | Thắng, thua, hoặc hết một ngày | Cấp đội, Notes và tiến độ run nằm trong hồ sơ. File ghi qua `GameMetaSaveLoad`. View không tự ghi đĩa | `GameMetaSaveData` | 10 |

**FR-01.** Ba mức khó chỉ là bộ nhân. On Beat giảm máu và đòn địch, tăng Notes. Off Beat tăng máu và đòn địch. Cadence là mức 1,00. Người chơi vẫn đặt chiêu bằng footprint S1, S, S2 ở cả ba mức. Vì vậy `DifficultyRuntime` trả hệ số, và `CombatSession` không có nhánh luật riêng cho từng mức.

**FR-02.** Một ngày có một quiz sáng và hai slot. `ConsumeActivitySlot` trừ một slot. `AdvanceDay` chỉ chạy khi ngày hiện tại đã dùng hết việc của nó. `DaysUntilVaultDeadline` đọc hạn vault từ `GameDate`, không đọc từ một nút trên bản đồ thành phố.

**FR-03.** `AddExp` cộng vào đúng trục người chơi vừa làm, rồi xét ngưỡng hạng của trục đó. Hạng tối đa là 10. Không có hàm nào trên `SocialStatsState` nhận một `UnitStats` để ghi đè sức đánh hoặc máu tối đa.

**FR-04.** Mỗi NPC trong Arc 1 có một dòng bond: Ren, Charlotte, Coda, Ryo, Mei Lin, Astra. Echo Key và trần hạng nằm trên `BondProgress`. Menu bond đọc dòng đó để vẽ. Menu không tự tăng hạng khi người chơi chỉ mở màn hình.

**FR-05.** `MapGenerator` tạo đồ thị một lần từ seed rồi rút lui. `CanTravelTo` là cửa duy nhất trước `EnterNode`. View được phép vẽ node chưa đi được. View không được ghi `Visited` khi người chơi bấm một node bị khóa.

**FR-06.** Vào trận tạo sân và người đứng trên sân. `PartyLoadoutApplicator` đọc cấp đội từ hồ sơ meta và gắn chiêu đã mở. Chiều ngược lại không có trong mã này: trận không nâng hạng đời sống.

**FR-07.** Planning là trạng thái `IsPlanningWindowOpen` đúng. Kéo quân đổi ô trên `DualGrid`. Thả chiêu gọi `TryAssignPlayerAction`. Prep, Delay và ReduceS2 ghi trên entry và trên unit trước khi người chơi bấm Execute. Đặt sai footprint trả về từ chối và hàng chờ giữ nguyên.

**FR-08.** Execute gọi `ConfirmPlanningAndExecute`. Session hỏi beat tuyệt đối, cộng mốc neo, rồi cho thước cục bộ chạy 22 beat. Intro 12 beat chỉ xảy ra một lần ở đầu trận, không lặp ở phase sau. 152 BPM, 677 beat và lookahead 3 nhịp là số của trận boss remix, nằm ở profile timeline và máy phát nhạc.

**FR-09.** Chắn là thao tác trong lúc Execute, không phải một chiêu trên kit. Resolver so thời điểm bấm với nhịp đòn. Unit trừ máu hoặc khiên sau kết quả đó. `OnUnitHpChanged` báo cho view. `CounterPresentationDriver` vẽ chip và chữ, rồi dừng.

**FR-10.** Hết trận phát `OnEncounterEnded`. Kinh nghiệm trận cộng vào cấp đội. Notes và đường đi trên map vào hồ sơ. Hết ngày thì lịch sang ngày mới trong cùng hồ sơ. Một đường `GameMetaSaveLoad` ghi cả lịch, bond, chỉ số đời sống và cấp đội. Bond tăng trong thoại vì vậy còn khi người chơi vào hầm.

Ba mã sau chưa đưa vào sơ đồ lớp. Chúng mô tả việc sẽ làm khi luật đã khóa, và lớp sẽ nhận việc đó.

**FR-11. Mở combat từ node battle và elite.** Hiện node boss đã gọi `RunMapSceneLoader` sang scene combat. Node battle và elite chưa mở trận theo cùng cửa đó. Khi làm, loader vẫn hỏi `RunState` trước, và vẫn không sinh lại `MapGraph`.

**FR-12. Hạn vault và Resonance Dive.** `CalendarState` đã có ngày và hạn. Cờ “vào hầm đúng hạn” và cảnh Resonance Dive ngày 05/09 chưa có lớp riêng. Khi làm, cờ nằm trên hồ sơ meta, không nằm trên `CombatSession`.

**FR-13. Kit boss Arc 1 có số cố định.** Astra, Mimi, Kiki và boss The Pulse còn mục GDD chưa khóa số. Khi khóa, mỗi đòn là một `SkillDefinitionSO` và một `EnemyTelegraph`. Không thêm một lớp trận thứ hai.

Cách kiểm một mã đang chạy: làm đúng việc ở cột thứ hai, rồi đọc trạng thái của lớp ở cột thứ tư. Khớp kết quả ở cột thứ ba thì mã đó đạt. Lệch kết quả thì sửa lớp đó, không sửa lớp vẽ cho đủ nhìn.
