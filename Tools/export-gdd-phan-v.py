# -*- coding: utf-8 -*-
"""Export GDD Roman V — He thong trong game."""
from pathlib import Path

from docx import Document
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml.ns import qn
from docx.shared import Cm, Pt, RGBColor

NAVY = RGBColor(0x1B, 0x2A, 0x4A)
CYAN = RGBColor(0x1A, 0x5F, 0x7A)


def set_run_font(run, size=12, bold=False, color=None, name="Times New Roman"):
    run.font.name = name
    run._element.rPr.rFonts.set(qn("w:eastAsia"), name)
    run.font.size = Pt(size)
    run.bold = bold
    if color is not None:
        run.font.color.rgb = color


def add_p(doc, text, *, size=12, bold=False, first_line=True, space_after=8, align=None):
    p = doc.add_paragraph()
    if align is not None:
        p.alignment = align
    pf = p.paragraph_format
    pf.space_after = Pt(space_after)
    pf.space_before = Pt(0)
    pf.line_spacing = 1.15
    if first_line:
        pf.first_line_indent = Cm(1)
    run = p.add_run(text)
    set_run_font(run, size=size, bold=bold)
    return p


def add_heading_vi(doc, text, level=1):
    p = doc.add_paragraph()
    pf = p.paragraph_format
    pf.space_before = Pt(16 if level == 1 else 12)
    pf.space_after = Pt(8)
    pf.first_line_indent = Cm(0)
    run = p.add_run(text)
    size = 16 if level == 1 else 14 if level == 2 else 13
    set_run_font(run, size=size, bold=True, color=NAVY)
    return p


def add_caption(doc, text):
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    pf = p.paragraph_format
    pf.space_before = Pt(6)
    pf.space_after = Pt(10)
    pf.first_line_indent = Cm(0)
    run = p.add_run(text)
    set_run_font(run, size=11, bold=True, color=CYAN)
    return p


def add_note(doc, text):
    p = doc.add_paragraph()
    pf = p.paragraph_format
    pf.space_after = Pt(8)
    pf.first_line_indent = Cm(0)
    run = p.add_run(text)
    set_run_font(run, size=11, bold=False, color=RGBColor(0x44, 0x44, 0x44))
    return p


def shade_cell(cell, hex_color):
    tc = cell._tc
    tcPr = tc.get_or_add_tcPr()
    shd = tcPr.makeelement(
        qn("w:shd"),
        {
            qn("w:fill"): hex_color,
            qn("w:val"): "clear",
        },
    )
    tcPr.append(shd)


def add_table(doc, headers, rows, caption):
    table = doc.add_table(rows=1 + len(rows), cols=len(headers))
    table.style = "Table Grid"
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    for i, h in enumerate(headers):
        cell = table.rows[0].cells[i]
        cell.text = ""
        p = cell.paragraphs[0]
        p.alignment = WD_ALIGN_PARAGRAPH.CENTER
        run = p.add_run(str(h))
        set_run_font(run, size=10, bold=True, color=RGBColor(0xFF, 0xFF, 0xFF))
        shade_cell(cell, "1B2A4A")
    for r_i, row in enumerate(rows):
        for c_i, val in enumerate(row):
            cell = table.rows[r_i + 1].cells[c_i]
            cell.text = ""
            p = cell.paragraphs[0]
            run = p.add_run(str(val))
            set_run_font(run, size=10)
            if r_i % 2 == 1:
                shade_cell(cell, "EEF3F8")
    add_caption(doc, caption)
    return table


def build():
    doc = Document()
    section = doc.sections[0]
    section.top_margin = Cm(2)
    section.bottom_margin = Cm(2)
    section.left_margin = Cm(2.5)
    section.right_margin = Cm(2)
    section.page_width = Cm(21)
    section.page_height = Cm(29.7)

    style = doc.styles["Normal"]
    style.font.name = "Times New Roman"
    style.font.size = Pt(12)
    style._element.rPr.rFonts.set(qn("w:eastAsia"), "Times New Roman")

    title = doc.add_paragraph()
    title.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = title.add_run("FRACTURED CHORUS")
    set_run_font(r, size=18, bold=True, color=NAVY)

    sub = doc.add_paragraph()
    sub.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = sub.add_run("Game Design Document — Mục V")
    set_run_font(r, size=14, bold=True, color=CYAN)

    add_p(
        doc,
        "Hệ thống trong game",
        size=16,
        bold=True,
        first_line=False,
        align=WD_ALIGN_PARAGRAPH.CENTER,
    )
    add_note(
        doc,
        "Tài liệu tách riêng cho chương V. Số liệu khóa mức Cadence (độ khó mặc định). "
        "Nguồn bảng: CHARACTER_LEVEL_PROGRESS, combat-level-xp-progression-design, "
        "COMBAT_MECHANICS, SKILL_KIT, DIFFICULTY, BOSS_ENCOUNTER_DESIGN, persona-calendar-design.",
    )

    add_heading_vi(doc, "V. Hệ thống trong game", 1)
    add_p(
        doc,
        "Chương này nói năm thứ đứng sau phần nhân vật.",
    )
    add_p(doc, "1. Chỉ số — đội mạnh hay yếu.", first_line=False)
    add_p(doc, "2. Chỉ số đời sống — gặp được ai, và hạng nào được mở.", first_line=False)
    add_p(doc, "3. Bản đồ hầm — đi lối nào.", first_line=False)
    add_p(doc, "4. Lịch ở thành phố — ngày nào vào hầm.", first_line=False)
    add_p(doc, "5. Quái — phải đọc đòn gì.", first_line=False)
    add_p(
        doc,
        "Năm mục này ăn khớp nhau. Thiếu một mục thì người chơi chỉ còn đánh, hoặc chỉ còn ở thành phố, hoặc chỉ còn bấm đúng nhịp. "
        "Cách một trận diễn ra, từ cửa nghĩ đến cửa chạy, thuộc chương VI. Đó là cơ chế đánh, không phải cách nâng chỉ số.",
    )
    add_p(
        doc,
        "Số liệu dưới đây là mức Cadence (độ khó mặc định). Hai mức kia chỉ đổi máu, sát thương và level quái. Luật chơi không đổi.",
    )

    add_table(
        doc,
        ["", "On Beat", "Cadence (mặc định)", "Off Beat"],
        [
            ["Lệch level địch", "−2", "0", "+2"],
            ["Cấp đội gợi ý trước boss", "13", "15", "17"],
            ["Cấp boss hiệu lực", "16", "18", "20 (scale; trần đội vẫn 18)"],
            ["Tỷ lệ máu địch", "×0,85", "×1,00", "×1,15"],
            ["Tỷ lệ đòn địch", "×0,85", "×1,00", "×1,20"],
            ["Đòn xuyên / hàng trước", "×0,80", "×1,00", "×1,15"],
            ["Notes sau trận", "×1,10", "×1,00", "×1,00"],
            ["Phạt đỡ lệch nhịp", "0", "0", "−0,10"],
            ["Máu địch sau khi gộp", "khoảng ×0,75", "×1,00", "khoảng ×1,29"],
            ["Đòn địch sau khi gộp", "khoảng ×0,77", "×1,00", "khoảng ×1,32"],
        ],
        "Bảng V.1 — Ba bậc độ khó (DIFFICULTY.md)",
    )

    # --- V.1 ---
    add_heading_vi(doc, "V.1. Chỉ số", 2)
    add_p(
        doc,
        "Chỉ số trong Fractured Chorus dùng để trả lời hai câu hỏi riêng. "
        "Câu thứ nhất là trong trận đánh, đội có chịu nổi đòn và có ra chiêu đúng nhịp hay không. "
        "Câu thứ hai là ở thành phố, Ren có gặp được người cần gặp và có mở được cảnh gắn kết hay không. "
        "Hai câu hỏi này dùng hai thanh điểm khác nhau, nên điểm của bên này không làm tăng bên kia.",
    )
    add_p(
        doc,
        "Trong trận, nhân vật mạnh lên theo ba cách, và cả ba cách đều gắn với cấp của cả đội. "
        "Khi cấp đội tăng, chỉ số của từng người tự tăng thêm một phần, theo đúng vai của người đó trong đội. "
        "Cùng lúc, người chơi nhận một điểm cộng tay cho mỗi người, rồi tự chọn bỏ điểm ấy vào STR, Ma, EN hoặc HB. "
        "Khi cấp đội chạm một mốc đã định trước, chiêu mới của người đó được mở, và việc mở chiêu không tốn điểm cộng tay. "
        "Ở thành phố, năm trục chỉ số đời sống tăng hạng khi Ren đi học, đi làm, nghỉ ngơi hoặc gặp người. "
        "Hạng đời sống không làm tăng máu và không làm tăng sát thương trong trận.",
    )
    add_p(
        doc,
        "Hai lớp điểm được tách riêng để mỗi buổi tối người chơi phải chọn một việc, và việc đó có hậu quả rõ. "
        "Nếu chọn vào hầm, đội nhận điểm kinh nghiệm trận và tiến gần tới boss hơn. "
        "Nếu ở lại thành phố, mối quan hệ với người khác và hạng chỉ số đời sống được tăng lên. "
        "Ngày 20/09 là hạn phải hoàn thành Vault, nên số buổi tối trước hạn không đủ để làm cả hai việc một cách thoải mái. "
        "Nếu hai lớp điểm được cộng chung, người chơi sẽ chỉ làm một việc cho đến khi số liệu đủ, rồi mới quay sang việc còn lại.",
    )

    add_heading_vi(doc, "V.1.1. Chỉ số trận", 3)
    add_p(
        doc,
        "Chỉ số trận gắn trên từng nhân vật. "
        "Người chơi theo dõi và cộng điểm vào bốn mục: Sức mạnh, viết tắt STR; Phép, viết tắt Ma; Nhịp tim, tên đầy đủ là HeartBeat, viết tắt HB; và Chịu đòn, tên đầy đủ là Endurance, viết tắt EN. "
        "Cả đội dùng chung một cấp, gọi là cấp đội. Vì vậy Ren, Charlotte và Coda lên cấp cùng một lúc, không ai lên cấp trước người khác.",
    )
    add_p(
        doc,
        "STR quyết định sức của các chiêu vật lý, đồng thời là nguồn dùng để tính máu của Ren và Charlotte. "
        "Ma quyết định sức của các chiêu phép. Coda gây sát thương và hồi máu cho đồng đội bằng Ma. "
        "Khi một chiêu được ghi là vật lý, sát thương của chiêu đó lấy từ STR. Khi một chiêu được ghi là phép, sát thương của chiêu đó lấy từ Ma. "
        "Trong trận, người chơi không chuyển một chiêu từ nguồn này sang nguồn kia.",
    )
    add_p(
        doc,
        "HB không làm tăng máu và không làm tăng sát thương. HB chi phối bốn việc xảy ra lúc nhân vật chuẩn bị chiêu. "
        "Người có HB cao hơn được đặt chiêu trước người có HB thấp hơn. Độ dài của cửa nghĩ chiêu, ký hiệu là W, cũng phụ thuộc vào HB. "
        "HB cao giúp đọc nốt của địch rõ hơn, nhưng mức rõ không giống nhau giữa ba người. "
        "Ren thấy được loại nốt. Coda thấy loại nốt và thấy thêm biểu tượng hiệu ứng của nốt đó. Charlotte chủ yếu thấy nốt đó có kèm đòn phụ hay không. "
        "Sau khi một chiêu chạy xong, người có HB cao được mở cửa nghĩ chiêu tiếp theo sớm hơn.",
    )
    add_p(
        doc,
        "Cửa nghĩ chiêu không dài thêm mỗi khi người chơi bỏ một điểm vào HB. Cửa ngắn nhất là 7 nhịp và dài nhất là 10 nhịp. "
        "Cách tính là lấy HB trừ 120, chia cho 26, lấy phần nguyên, rồi cộng 7. "
        "Nếu kết quả nhỏ hơn 7 thì cửa vẫn là 7 nhịp. Nếu kết quả lớn hơn 10 thì cửa dừng ở 10 nhịp. "
        "Ren ở cấp 1 có HB bằng 145, nên cửa của cậu vẫn là 7 nhịp. Khi HB đạt 146, cửa tăng thành 8 nhịp. "
        "Với Ren, mốc 146 đến từ phần tăng tự động quanh cấp 3, nên cậu có cửa 8 nhịp mà chưa cần bỏ điểm cộng tay vào HB. "
        "Muốn cửa dài 9 nhịp, HB phải đạt khoảng 172. Trong cách cộng điểm dùng để cân boss, chỉ Ren ở cấp 18 mới đạt cửa 9 nhịp. "
        "Charlotte giữ cửa 7 nhịp suốt phần truyện đầu, vì HB gốc của cô thấp và phần tăng thêm không đủ để vượt mốc kế tiếp.",
    )
    add_p(
        doc,
        "Độ trễ sau chiêu cũng đổi theo từng mốc HB, chứ không giảm đều sau mỗi điểm cộng tay. "
        "Khi HB thấp hơn 85, nhân vật phải chờ 2 nhịp thì cửa nghĩ tiếp theo mới mở. "
        "Khi HB từ 85 đến 169, thời gian chờ là 1 nhịp. Khi HB từ 170 trở lên, cửa nghĩ mở ngay và không phải chờ. "
        "Ở cấp 15, cả ba người đều phải chờ 1 nhịp. Điểm HB ở cấp này vẫn có ích, vì người được cộng HB sẽ nghĩ chiêu trước và đọc nốt địch rõ hơn, dù cửa chưa dài thêm. "
        "Cửa chỉ dài thêm một nhịp khi HB vượt một mốc trong công thức ở trên. Trong phần truyện đầu, cửa 9 nhịp chỉ xuất hiện ở Ren khi cậu đạt cấp 18.",
    )
    add_p(
        doc,
        "EN không làm tăng máu tối đa. EN làm cho mỗi đòn địch đánh trúng gây ít sát thương hơn, và làm cho lần đỡ bằng phím Space chặn được nhiều sát thương hơn. "
        "Vì vậy nhân vật có ít máu vẫn cần được cộng EN. "
        "Máu cho biết nhân vật chịu được tổng bao nhiêu sát thương trước khi ngã. EN cho biết mỗi nhịp bị trúng sẽ lấy đi bao nhiêu máu.",
    )
    add_p(
        doc,
        "Điểm kinh nghiệm trận chỉ được cộng khi thắng trận thường, trận khó hoặc trận boss. "
        "Ô sự kiện, ô nghỉ và cửa hàng trong hầm không cho loại điểm này. "
        "Khi thanh điểm đủ, cả đội lên một cấp. Mỗi người nhận đúng một điểm cộng tay, và điểm đó chỉ được bỏ cho chính người đó, vào một trong bốn mục STR, Ma, EN hoặc HB. "
        "Một điểm bỏ vào STR, Ma hoặc EN làm mục đó tăng 1. Một điểm bỏ vào HB làm HB tăng 5. "
        "Mỗi mục chỉ nhận tối đa 10 điểm cộng tay. Ở cấp 15 mỗi người đã có 14 điểm, nên không thể dồn hết vào một mục. Điểm phải chia cho ít nhất hai mục.",
    )
    add_table(
        doc,
        ["Chỉ số", "Vai trò"],
        [
            ["STR", "Máu của đội · sát thương đánh tay và chiêu vật lý"],
            ["Ma", "Sát thương chiêu phép"],
            ["HB (Nhịp tim)", "Thứ tự nghĩ chiêu · độ dài cửa nghĩ · đọc đòn địch · độ trễ sau chiêu"],
            ["EN (Chịu đòn)", "Giảm sát thương nhận vào"],
        ],
        "Bảng V.2 — Vai trò chỉ số trận (BOSS_ENCOUNTER_DESIGN.md)",
    )
    add_note(doc, "[Hình 76 — Menu chỉ số trận của nhân vật]")
    add_p(
        doc,
        "Bảng V.2 cố ý chỉ ghi vai trò của từng chỉ số, không ghi công thức. "
        "Bốn dòng tương ứng với bốn câu hỏi khi đánh: chiêu vật lý có mạnh không, chiêu phép có mạnh không, nhân vật có kịp nghĩ chiêu không, và đòn trúng vào có bị giảm hay không. "
        "Máu không có dòng riêng để người chơi cộng điểm. Máu được tính ra từ STR. Với Coda, công thức máu còn cộng thêm một phần của Ma.",
    )

    add_heading_vi(doc, "V.1.2. Chỉ số đời sống", 3)
    add_p(
        doc,
        "Chỉ số đời sống gắn với lịch sinh hoạt của Ren khi cậu ở thành phố. Charlotte và Coda không có bộ năm trục này. "
        "Năm trục gồm Resonance, Cadence, Pulse, Harmony và Rhythm. "
        "Ở mục này, Cadence là tên của một trục xã hội, đứng cùng nhóm với Resonance và Pulse. "
        "Game còn có một bậc khó cũng mang tên Cadence. Bậc khó đó chỉ thay đổi máu và sát thương của địch. "
        "Trục Cadence trong chỉ số đời sống dùng để mở hoạt động học và một số tuyến gặp người.",
    )
    add_p(
        doc,
        "Mỗi trục có hạng từ 1 đến 10. Khi Ren làm một việc trong ngày, việc đó cộng điểm kinh nghiệm đời sống vào trục tương ứng. "
        "Hạng chỉ tăng sau khi điểm tích lũy đạt ngưỡng của hạng kế tiếp, chứ không tăng ngay sau một lần làm việc. "
        "Ngưỡng mặc định bắt đầu từ 15 điểm và tăng dần. Ngưỡng cao nhất là 120 điểm. "
        "Số điểm chính xác của từng hạng được để trong dữ liệu của game, để còn chỉnh sau khi chơi thử. "
        "Vì ngưỡng tăng dần, một lần ghé shop hoa chưa đủ để mở điều kiện gặp người. Ren phải lặp cùng một loại việc vài lần thì hạng mới tăng.",
    )
    add_table(
        doc,
        ["Trục", "Tương đương (Persona)", "Mở gắn kết / việc"],
        [
            ["Resonance", "Charm", "Melody, Pulse; shop hoa"],
            ["Cadence", "Knowledge", "Harmony, Measure; học, câu hỏi lớp"],
            ["Pulse", "Guts", "Bass, Crescendo; luyện nhạc"],
            ["Harmony", "Proficiency", "Harmony, Rest; làm thêm"],
            ["Rhythm", "Courage", "Dissonance, Fermata; nghỉ"],
        ],
        "Bảng V.3 — Năm trục đời sống (persona-calendar-design §5)",
    )
    add_note(doc, "[Hình 77 — Menu chỉ số đời sống]")
    add_p(
        doc,
        "Một ngày được chia thành ba khoảng thời gian, nhưng chỉ khoảng ban ngày và buổi tối mới tiêu một lượt hoạt động. "
        "Buổi sáng là câu hỏi trên lớp. Nếu trả lời đúng, Ren được cộng điểm đời sống theo chủ đề của câu hỏi, thường là 8 điểm, và buổi sáng không làm mất lượt ban ngày. "
        "Ban ngày, người chơi chọn một việc. Học ở thư viện cộng 8 điểm Cadence. Luyện nhạc cộng 8 điểm Harmony hoặc 8 điểm Pulse, tùy việc được chọn. "
        "Ngồi quán cà phê cộng 8 điểm Resonance. Nghỉ ngơi cộng 5 điểm Rhythm. "
        "Làm thêm ở cửa hàng tiện lợi cộng 10 điểm Harmony và 4 điểm Cadence. Làm ở shop hoa cộng 10 điểm Resonance và 4 điểm Harmony. "
        "Buổi tối cũng chỉ chọn được một việc. Nếu vào hầm, buổi tối đó bị dùng hết, lịch thành phố tạm dừng trong lúc chạy hầm, và đội nhận điểm kinh nghiệm trận từ các trận thắng. "
        "Nếu ở lại thành phố, buổi tối được dùng để tiếp tục tăng chỉ số đời sống hoặc để đi gặp người.",
    )
    add_p(
        doc,
        "Hạng chỉ số đời sống là điều kiện để được gặp một số người, và là điều kiện để tăng hạng gắn kết với họ. "
        "Muốn tăng hạng gắn kết, người chơi cần đủ điểm gắn kết, đủ hạng ở những trục mà người đó yêu cầu, và trong một số trường hợp còn cần đúng một sự kiện đã xảy ra trong cốt truyện. "
        "Theo bảng khóa hiện tại, Ren cần Resonance và Pulse, Charlotte cần Pulse và Rhythm, Coda cần Cadence và Harmony, Astra cần Resonance và Harmony, Ryo cần Cadence và Rhythm, còn Mei Lin cần Cadence và Resonance. "
        "Khi đủ các điều kiện này, cảnh gặp người được mở. Một số lần tăng hạng gắn kết còn cho một quyền lợi mang vào các lần vào hầm sau. "
        "Quyền lợi đó đến từ hạng gắn kết đã mở. Năm trục đời sống không được cộng thẳng vào STR hoặc Ma.",
    )
    add_p(
        doc,
        "Nếu việc gặp bạn cũng cho điểm kinh nghiệm trận, người chơi sẽ ở lại thành phố để làm đội mạnh, rồi chỉ vào hầm khi cốt truyện buộc phải đi. Lúc đó hầm không còn là nơi để luyện cấp. "
        "Nếu việc đánh trong hầm cũng làm tăng hạng đời sống, người chơi sẽ dành mọi buổi tối cho hầm. Lịch thành phố khi đó ít còn đáng để dành thời gian, vì người chơi không cần buổi chiều hoặc buổi tối để gặp ai. "
        "Vì hai lớp điểm được tách riêng, từ ngày 7/09 đến ngày 20/09 mỗi buổi tối chỉ chọn được một trong hai hướng: vào hầm, hoặc ở lại thành phố để gặp người và tăng hạng. "
        "Ban ngày vẫn làm được việc ở thành phố. Buổi tối không làm được cả hai việc cùng một lúc.",
    )

    add_heading_vi(doc, "V.1.3. Cách một cấp làm người đó mạnh lên", 3)
    add_p(
        doc,
        "Khi cấp đội tăng thêm một cấp, có ba việc xảy ra theo thứ tự sau. "
        "Trước hết, phần tăng tự động được cộng vào chỉ số của từng người, kể cả khi người chơi chưa bỏ điểm cộng tay. "
        "Tiếp theo, mỗi người nhận một điểm cộng tay. "
        "Sau cùng, nếu cấp vừa đạt trùng với mốc mở chiêu, chiêu đó xuất hiện, và game không trừ điểm cộng tay vừa nhận để đổi lấy chiêu.",
    )
    add_p(
        doc,
        "Mỗi chỉ số ở một cấp được tính bằng chỉ số gốc ở cấp 1, cộng với mức tăng mỗi cấp nhân với số cấp đã lên, rồi cộng thêm phần đã bỏ từ điểm cộng tay. Số cấp đã lên bằng cấp hiện tại trừ 1. "
        "Một điểm cộng tay bỏ vào STR, Ma hoặc EN làm mục đó tăng 1. Một điểm bỏ vào HB làm HB tăng 5. "
        "Lấy Ren làm ví dụ. Ở cấp 1, cậu có STR bằng 22 và chưa có điểm cộng tay. Khi lên cấp 2, STR của cậu tự thành 23, vì STR của cậu tăng thêm 1 sau mỗi cấp. "
        "Nếu điểm cộng tay của cấp 2 được bỏ vào EN, STR vẫn giữ ở 23. Muốn STR cao hơn mức tự tăng, người chơi phải bỏ điểm vào STR.",
    )
    add_table(
        doc,
        ["Bỏ 1 điểm vào", "Được thêm", "Tác động"],
        [
            ["STR", "+1", "Đòn vật lý · Ren/Coda +2 máu · Charlotte +6 máu"],
            ["Ma", "+1", "Đòn phép và hồi máu của Coda"],
            ["EN", "+1", "Chịu đòn tốt hơn"],
            ["HB", "+5 HB", "Nghĩ chiêu trước · đọc nốt rõ hơn · tiến tới mốc cửa dài và bớt trễ"],
        ],
        "Bảng V.4 — Cộng 1 điểm thì được gì (CHARACTER_LEVEL_PROGRESS.md)",
    )
    add_p(
        doc,
        "Bảng V.4 là chỗ dễ đọc nhầm, vì cột ghi phần được thêm không có nghĩa giống nhau với mọi người. "
        "Một điểm HB thành thêm 5 HB, nên trông như một lần tăng lớn, nhưng cửa nghĩ chiêu không dài thêm 1 nhịp sau mỗi điểm đó. "
        "Một điểm STR của Charlotte thành thêm 6 máu, trong khi cùng một điểm STR của Ren hoặc của Coda chỉ thành thêm 2 máu. "
        "Cách quy đổi khác nhau là có chủ đích. Cùng một điểm cộng tay, mỗi người nhận lại thứ mà vai của họ cần trong trận.",
    )
    add_p(
        doc,
        "Máu không có mục riêng trên màn cộng điểm. Máu được tính từ STR, và với Coda thì còn tính thêm một phần Ma. "
        "Máu của Ren bằng STR nhân 2, rồi cộng 30. "
        "Máu của Charlotte bằng STR nhân 6, rồi cộng 50. Hệ số 6 được đặt vì cô đứng hàng trước và phải chịu những đòn xuyên qua hàng trước. "
        "Nếu lấy STR 35 của cô ở cấp 15 mà tính theo công thức của Ren, cô chỉ có 100 máu. Với hệ số 6, cô có 260 máu, đủ để đứng hàng trước. "
        "Máu của Coda bằng STR nhân 2, cộng với Ma nhân 0,35, rồi cộng 15. Hệ số 0,35 được đặt nhỏ có chủ đích. "
        "Coda vì vậy vẫn ít máu và cần đứng hàng sau. Khi người chơi bỏ điểm vào Ma, cô vẫn được thêm một ít máu, đồng thời đòn phép và lượng hồi máu của cô cũng tăng.",
    )

    add_table(
        doc,
        ["", "STR", "Ma", "HB", "EN", "HP", "W"],
        [
            ["Ren", "22", "6", "145", "4", "74", "7"],
            ["Charlotte", "15", "5", "105", "10", "140", "7"],
            ["Coda", "6", "30", "125", "3", "38", "7"],
        ],
        "Bảng V.5 — Chỉ số gốc cấp 1 (combat-level-xp-progression-design §3)",
    )
    add_p(
        doc,
        "Các chỉ số gốc ở cấp 1 đã phân vai trước khi người chơi bỏ bất kỳ điểm cộng tay nào. "
        "Ren vào trận với STR bằng 22 và 74 máu, nên cậu gây sát thương được ngay nhưng lượng máu còn thấp. "
        "Charlotte vào trận với EN bằng 10 và 140 máu, nên cô chịu đòn tốt ngay từ trận đầu. "
        "Coda vào trận với Ma bằng 30 và 38 máu, nên sức phép của cô đã có từ đầu nhưng máu của cô rất thấp. "
        "Cửa nghĩ chiêu của cả ba người ở cấp 1 đều dài 7 nhịp. Sự khác nhau lúc mới vào game nằm ở sức đánh và lượng máu, chưa nằm ở độ dài cửa nghĩ.",
    )
    add_table(
        doc,
        ["", "Ren", "Charlotte", "Coda"],
        [
            ["STR", "+1,0", "+1,0", "+1,0"],
            ["Ma", "+0,2", "+0,1", "+1,0"],
            ["HB", "+0,5", "+0,5", "+0,5"],
            ["EN", "+0,2", "+0,3", "+0,2"],
        ],
        "Bảng V.6 — Tăng tự động mỗi cấp (CHARACTER_LEVEL_PROGRESS.md)",
    )
    add_p(
        doc,
        "Phần tăng tự động giúp nhân vật vẫn phát triển đúng vai của mình, kể cả khi người chơi bỏ điểm cộng tay khác với bảng gợi ý. "
        "Cả ba người đều được cộng thêm 1 STR sau mỗi cấp, nên sức đánh vật lý vẫn tăng dần. "
        "Coda được cộng thêm 1 Ma sau mỗi cấp, nên cô vẫn trở thành người dùng phép dù điểm cộng tay bị bỏ vào mục khác. "
        "Charlotte chỉ được cộng thêm 0,1 Ma sau mỗi cấp, vì phép không phải vai của cô trong trận. "
        "EN là mục tăng chậm nhất. Ren và Coda thêm 0,2 EN mỗi cấp, còn Charlotte thêm 0,3 EN mỗi cấp. Đây là lý do điểm cộng tay cần được bù vào EN. "
        "Nếu chỉ dựa vào phần tăng tự động, EN lúc tới boss vẫn thấp so với lượng máu và sát thương của trận đó. "
        "HB của cả ba người chỉ thêm 0,5 sau mỗi cấp, nên cửa nghĩ chiêu dài thêm rất chậm nếu người chơi không bỏ điểm vào HB. "
        "Bảng cấp đầy đủ còn có cột may mắn và cột hệ số chí mạng. Hai cột này tăng theo cấp đội. Người chơi không bỏ điểm cộng tay vào hai cột này.",
    )
    add_table(
        doc,
        ["Cấp", "Ren", "Charlotte", "Coda"],
        [
            ["1", "Strike", "Ram", "Pulse"],
            ["3", "—", "Anchor", "—"],
            ["4", "Crosscut", "—", "—"],
            ["5", "—", "—", "Mend"],
            ["9", "—", "Bulwark", "—"],
            ["10", "Finale", "—", "—"],
            ["11", "—", "—", "Encore"],
        ],
        "Bảng V.7 — Cấp nào mở chiêu nào (SKILL_KIT.md)",
    )
    add_p(
        doc,
        "Game không có một khoản điểm riêng để mua chiêu. Khi cấp đội tới mốc, chiêu được mở, còn điểm cộng tay vẫn dùng để tăng chỉ số. "
        "Người chơi vì vậy không phải chọn giữa việc mở chiêu và việc tăng máu. "
        "Nếu buộc phải chọn như vậy, người chơi có thể hoãn chiêu cuối Finale để giữ điểm tăng máu, rồi đánh boss khi bộ chiêu vẫn chưa đủ. "
        "Boss ở tầng 16 được cân cho đội đã mở đủ ba chiêu của mỗi người. Lịch mở chiêu được cố định để người chơi học cách đánh theo đúng thứ tự, từ chiêu thường đến chiêu giữa, rồi đến chiêu cuối.",
    )
    add_p(
        doc,
        "Ở cấp 1 và cấp 2, mỗi người chỉ có chiêu thường, cùng với cách đỡ đòn bằng phím Space. Đoạn này dùng để người chơi học cách đặt chiêu đúng nhịp. "
        "Từ cấp 3 đến cấp 5, các chiêu giữa được mở, gồm Anchor của Charlotte, Crosscut của Ren và Mend của Coda. Từ đây trận đánh có thêm bước chuẩn bị và bước phản đòn. "
        "Từ cấp 9 đến cấp 11, các chiêu cuối được mở lần lượt, gồm Bulwark, Finale, rồi Encore. Trước khi đội vào các tầng gần boss, mỗi người đã có đủ ba chiêu. "
        "Từ cấp 12 đến cấp 15 không có chiêu mới. Ở đoạn này người chơi chỉ cộng chỉ số, để chỉnh máu, sát thương và cửa nghĩ chiêu trước khi gặp boss.",
    )

    add_heading_vi(doc, "V.1.4. Ba người cộng khác nhau", 3)
    add_p(
        doc,
        "Ba công thức máu ở trên chỉ hợp lý khi ba người không được cộng điểm theo cùng một kiểu. "
        "Charlotte là người đỡ đòn, nên điểm của cô cần đổi thành máu. Ren là người đánh chính, nên điểm của cậu cần bỏ vào STR. Coda là người hỗ trợ, nên điểm của cô cần bỏ vào Ma. "
        "Nếu cộng giống nhau cho cả ba chỉ để dễ nhớ, Charlotte sẽ không còn đủ máu để đứng hàng trước, hoặc Coda sẽ không còn đủ phép để hỗ trợ đội.",
    )
    add_p(
        doc,
        "Ren là người đánh chính, thuộc hệ Giai điệu, và các chiêu của cậu là chiêu vật lý. Ở cấp 1 cậu có 74 máu. "
        "Nếu cộng điểm theo cách dùng để cân boss, tới cấp 15 cậu có STR bằng 42 và có 114 máu. So với Charlotte, cậu vẫn là người ít máu trong đội. "
        "Nếu cậu đứng sai vị trí, hoặc để một nốt của địch đánh trúng, cậu sẽ ngã sớm hơn Charlotte rất nhiều. "
        "Vì vậy điểm cộng tay của cậu không nên dồn hết vào sát thương. Một phần cần bỏ vào EN, để mỗi nốt lọt qua hàng đỡ lấy đi ít máu hơn.",
    )
    add_note(doc, "[Hình 78 — Icon hệ Giai điệu (Melody)]")
    add_p(
        doc,
        "Charlotte là người đỡ đòn, thuộc hệ Nhịp, và các chiêu của cô là chiêu vật lý. Ở cấp 1 cô có 140 máu và EN bằng 10. "
        "Nếu cộng điểm đúng cách tới cấp 15, cô có 260 máu. Tổng máu của cả đội lúc đó là 447, nên phần máu của cô lớn hơn một nửa tổng máu đội. "
        "Cô đứng hàng trước, chịu các đòn xuyên qua hàng trước, và dùng chiêu Anchor để đẩy nốt của địch. "
        "Điểm STR bỏ cho cô có hiệu quả rõ, vì mỗi điểm STR tăng 6 máu cho cô, trong khi cùng một điểm đó chỉ tăng 2 máu cho Ren và cho Coda.",
    )
    add_note(doc, "[Hình 79 — Icon hệ Nhịp (Rhythm)]")
    add_p(
        doc,
        "Coda là người hỗ trợ, thuộc hệ Hòa âm, và các chiêu của cô là chiêu phép. Ở cấp 1 cô có Ma bằng 30, nhưng chỉ có 38 máu. "
        "Nếu cộng điểm đúng cách tới cấp 15, cô có Ma bằng 50 và có 73 máu. Cô vẫn là người ít máu nhất trong đội, nên vị trí của cô là hàng sau. "
        "Chiêu Mend hồi máu dựa trên phép của cô. Khi điểm cộng tay được bỏ vào Ma, đòn phép của cô tăng, lượng hồi máu tăng, và máu tối đa cũng tăng thêm một ít nhờ hệ số 0,35 trong công thức máu. "
        "Nếu bỏ điểm đó vào STR, máu của cô có tăng, nhưng sức phép và khả năng hồi máu không tăng theo.",
    )
    add_note(doc, "[Hình 80 — Icon hệ Hòa âm (Harmony)]")
    add_table(
        doc,
        ["", "STR", "Ma", "HB", "EN", "HP", "W", "Độ trễ"],
        [
            ["Ren", "42", "8,8", "167", "11,8", "114", "8", "1"],
            ["Charlotte", "35", "6,4", "127", "19,2", "260", "7", "1"],
            ["Coda", "20", "50", "147", "10,8", "73", "8", "1"],
        ],
        "Bảng V.8 — Ba người ở cấp 15 khi cộng đúng (dùng để cân boss)",
    )
    add_p(
        doc,
        "Bảng V.8 là mức chỉ số dùng để cân boss ở tầng 16. Đây là mức tham chiếu cho người thiết kế, không phải mức người chơi bắt buộc phải đạt đúng từng con số. "
        "Ở mức này, độ trễ sau chiêu của cả ba người vẫn là 1 nhịp. Cửa nghĩ của Ren và của Coda dài 8 nhịp, còn cửa của Charlotte dài 7 nhịp. "
        "Chỗ khác nhau lớn nhất là lượng máu: Ren có 114 máu, Charlotte có 260 máu và Coda có 73 máu. Boss tầng 16 được viết cho một đội có lượng máu và sức đánh như bảng này. "
        "Nếu đội vào boss sớm hơn cấp 15, máu và sát thương sẽ thấp hơn bảng. "
        "Nếu đội vào boss muộn hơn cấp 15, cần xem mục cấp đội ở phần dưới, vì sau cấp 15 điểm kinh nghiệm lấy từ hầm chỉ còn bằng 12% mức gốc.",
    )
    add_table(
        doc,
        ["Người", "14 điểm đến cấp 15", "3 điểm thêm đến cấp 18"],
        [
            ["Ren", "6 STR / 3 HB / 5 EN", "+1 EN → +1 STR → +1 HB"],
            ["Charlotte", "6 STR / 3 HB / 5 EN", "+1 EN → +1 STR → +1 HB"],
            ["Coda", "6 Ma / 3 HB / 5 EN", "+1 EN → +1 Ma → +1 HB"],
        ],
        "Bảng V.9 — Cách cộng điểm gợi ý",
    )
    add_p(
        doc,
        "Ở cấp 1, mỗi người chưa có điểm cộng tay. Khi đạt cấp 15, mỗi người đã nhận 14 điểm. Khi đạt cấp 18, mỗi người đã nhận 17 điểm. "
        "Mỗi mục chỉ nhận tối đa 10 điểm, nên 14 điểm của cấp 15 không thể bỏ hết vào một mục. "
        "Cách cộng điểm dùng để cân boss chia 14 điểm này thành ba phần: 6 điểm vào chỉ số đúng vai, 3 điểm vào HB và 5 điểm vào EN. "
        "Chỉ số đúng vai của Ren và của Charlotte là STR. Chỉ số đúng vai của Coda là Ma.",
    )
    add_p(
        doc,
        "Sáu điểm bỏ vào chỉ số đúng vai giúp người đó giữ được việc của mình cho đến lúc gặp boss. "
        "Với Ren, sáu điểm đó tăng sát thương vật lý. Với Charlotte, sáu điểm đó tăng máu để cô đứng hàng trước. Với Coda, sáu điểm đó tăng phép và hồi máu. "
        "Năm điểm EN dùng để bù cho phần EN tự tăng quá chậm. Ren bắt đầu với EN bằng 4, còn Coda bắt đầu với EN bằng 3. "
        "Nếu không bù EN, những nốt địch đánh lọt ở các tầng sau sẽ gây sát thương nặng hơn mức mà bảng cân boss đang lấy làm mốc. "
        "Ba điểm HB tương đương thêm 15 HB. Với Coda, mức tăng này đủ để cửa nghĩ chiêu từ 7 nhịp lên 8 nhịp khi cô đạt cấp 15. "
        "Với Ren, cửa đã lên 8 nhịp nhờ phần tăng tự động từ khá sớm, nên 15 HB thêm ở đoạn này chưa làm cửa dài thêm một nhịp nữa. Với Charlotte, cửa vẫn dài 7 nhịp. "
        "Dù cửa chưa dài thêm với mọi người, cả ba vẫn được nghĩ chiêu sớm hơn và đọc nốt địch rõ hơn. "
        "Ba điểm còn lại, nhận khi lên cấp 16, cấp 17 và cấp 18, được bỏ lần lượt vào EN, vào chỉ số đúng vai, rồi vào HB. "
        "Khi Ren nhận điểm HB cuối cùng này, HB của cậu đạt 173,5. Cửa nghĩ của cậu dài 9 nhịp, và độ trễ sau chiêu giảm về 0.",
    )
    add_p(
        doc,
        "Thứ tự bỏ điểm trong bảng cấp đầy đủ không phải là bỏ hết 6 điểm đúng vai ngay từ đầu. "
        "Điểm đầu tiên, nhận khi lên cấp 2, được bỏ vào EN, vì lúc đó đội còn chịu đòn kém và mới chỉ có một điểm để dùng. "
        "Các cấp ở giữa được xen điểm STR hoặc điểm Ma, để sát thương không đứng yên suốt nửa quãng đường trong hầm. "
        "Điểm HB được bỏ khi lên cấp 7, cấp 10 và cấp 15. Ba lần này được đặt cách nhau, vì HB chỉ đổi cách đánh một cách rõ khi tích đủ một mốc, chứ không đổi rõ sau từng điểm lẻ. "
        "Người chơi vẫn có thể bỏ điểm khác với bảng này. Bảng là đường dùng để cân boss, không phải quy tắc cấm người chơi chọn hướng khác.",
    )

    add_heading_vi(doc, "V.1.5. Cấp đội và điểm kinh nghiệm trận", 3)
    add_p(
        doc,
        "Cả đội dùng chung một thanh điểm kinh nghiệm trận. Không có chuyện một người lên cấp trước và người khác bị bỏ lại phía sau. "
        "Khi thanh điểm đầy, ba người cùng lên một cấp, cùng nhận một điểm cộng tay, và cùng được cộng phần tăng tự động của cấp đó. "
        "Cấp 15 có nghĩa là mỗi người đã nhận 14 điểm cộng tay. Cấp 18 có nghĩa là mỗi người đã nhận 17 điểm. "
        "Cấp 18 là mức cao nhất trong phần truyện đầu. Phần điểm vượt quá cấp 18 bị bỏ và không được giữ lại.",
    )
    add_p(
        doc,
        "Đường đi mà phần thiết kế mong người chơi đi được ghi khá rõ. "
        "Người chơi leo hầm từ tầng 1 đến tầng 15, thắng các ô có trận đánh trên đường đi, đưa đội tới khoảng cấp 15, rồi mới gặp boss ở tầng 16. "
        "Boss này được cân cho đội cấp 15, với 14 điểm đã cộng theo bảng ở mục trên. "
        "Lần đầu đánh bại boss, đội nhận 12.600 điểm kinh nghiệm trận. Con số này bằng đúng tổng điểm cần để đi từ cấp 15 lên cấp 18. "
        "Nếu vào boss khi đội vừa đạt cấp 15 và thanh điểm của cấp tiếp theo còn trống, thì sau khi thắng boss đội sẽ ở cấp 18. "
        "Đây là đường lên mức cao nhất của phần truyện đầu. Người chơi không cần ở lại hầm để đánh thêm cho đủ cấp cuối.",
    )
    add_p(
        doc,
        "Sau khi đội đạt cấp 15, điểm kinh nghiệm từ các ô trong hầm được nhân với 0,12, rồi lấy phần nguyên. "
        "Một trận khó ở tầng 15 vốn cho 1.200 điểm. Sau khi nhân 0,12, trận đó chỉ còn khoảng 144 điểm. "
        "Để lên từ cấp 15 lên cấp 16 cần 3.600 điểm, tức là khoảng 25 trận khó như vậy cho riêng một cấp. "
        "Hạn hoàn thành Vault là ngày 20/09, và mỗi ngày chỉ vào hầm được một lần vào buổi tối, nên việc lên cấp bằng cách đánh đi đánh lại trong hầm được làm chậm có chủ đích. "
        "Nếu cấp đội cao hơn cấp gợi ý của tầng đó quá hai bậc, số điểm vừa tính còn bị nhân thêm với 0,5. "
        "Vì vậy, quay lại đánh các tầng đầu khi đội đã cấp cao thì điểm nhận được còn rất ít. "
        "Phần thưởng của boss ở lần thắng đầu không bị hai lần nhân này. Nếu đánh bại boss thêm lần nữa trong phần truyện đầu, đội nhận 0 điểm kinh nghiệm trận. "
        "Game không tạo một vòng đánh boss nhiều lần chỉ để lấy cấp.",
    )
    add_p(
        doc,
        "Nếu vào boss khi đội chưa tới cấp 15, lần thắng đầu vẫn nhận đủ 12.600 điểm. "
        "Đội có thể dừng ở dưới cấp 18, vì còn thiếu những điểm lẽ ra đã nhận được trước khi gặp boss. "
        "Nếu đội đã vượt cấp 15, phần thưởng boss sẽ lấp thanh điểm cho đến khi đội đạt cấp 18, và phần điểm dư sau đó bị bỏ.",
    )
    add_p(
        doc,
        "Ba bậc độ khó không làm đổi đường lên cấp này. "
        "Bậc On Beat gợi ý đội vào boss khi đang khoảng cấp 13. Bậc Cadence gợi ý cấp 15. Bậc Off Beat gợi ý cấp 17. Mức cao nhất của đội vẫn là cấp 18 ở cả ba bậc. "
        "Off Beat làm địch có nhiều máu hơn và gây sát thương cao hơn, nhưng không tăng điểm kinh nghiệm trận. Chọn bậc khó vì vậy không phải là cách để lên cấp nhanh hơn. "
        "On Beat làm địch yếu hơn, và điểm kinh nghiệm trận vẫn được giữ nguyên. "
        "Người chơi chọn độ khó theo mức nặng hay nhẹ của trận đánh. Đường lên cấp không đổi theo bậc khó. Các tỷ lệ máu và sát thương của địch nằm ở Bảng V.1.",
    )
    add_table(
        doc,
        ["Khóa", "Giá trị"],
        [
            ["Mục tiêu vừa phải", "Hầm tầng 1–15 → đội cấp 15 trước boss tầng 16"],
            ["Cảnh boss", "Cân cho cấp 15, đã cộng 14 điểm"],
            ["Sau cấp 15", "Đánh hầm vẫn lên được cấp 16–18, nhưng điểm ×0,12"],
            ["Giết boss lần đầu", "+12.600 điểm trận (= tổng cấp 15→18)"],
            ["Trần phần truyện đầu", "Cấp 18"],
        ],
        "Bảng V.10 — Khóa cấp đội (combat-level-xp-progression-design §1)",
    )
    add_table(
        doc,
        ["Từ → đến", "Điểm cần", "Tổng để tới cấp đó", "Ghi chú"],
        [
            ["1→2", "60", "60", ""],
            ["2→3", "90", "150", ""],
            ["3→4", "130", "280", ""],
            ["4→5", "180", "460", ""],
            ["5→6", "240", "700", ""],
            ["6→7", "310", "1010", ""],
            ["7→8", "390", "1400", ""],
            ["8→9", "480", "1880", ""],
            ["9→10", "580", "2460", ""],
            ["10→11", "690", "3150", ""],
            ["11→12", "810", "3960", ""],
            ["12→13", "940", "4900", ""],
            ["13→14", "1080", "5980", ""],
            ["14→15", "1230", "7210", "Mục tiêu vừa phải"],
            ["15→16", "3600", "10810", "Bắt đầu chậm"],
            ["16→17", "4200", "15010", ""],
            ["17→18", "4800", "19810", "Trần phần truyện đầu"],
        ],
        "Bảng V.11 — Điểm để lên từng cấp (Σ 1→15 = 7210 · Σ 15→18 = 12600)",
    )
    add_p(
        doc,
        "Những cấp đầu cần ít điểm, để người chơi thấy được vòng lên cấp ngay trong vài buổi tối đầu tiên. Từ cấp 1 lên cấp 2 chỉ cần 60 điểm. "
        "Sau đó, mỗi cấp cần nhiều điểm hơn cấp ngay trước nó. Tổng điểm từ cấp 1 đến cấp 15 là 7.210 điểm. "
        "Tổng điểm từ cấp 15 đến cấp 18 là 12.600 điểm, đúng bằng phần thưởng khi thắng boss lần đầu. "
        "Bậc từ cấp 14 lên cấp 15 cần 1.230 điểm, rồi bậc từ cấp 15 lên cấp 16 cần 3.600 điểm. Mức tăng đột ngột này cho thấy việc ở lại hầm để tìm cấp cuối sẽ rất chậm.",
    )
    add_table(
        doc,
        ["Cụm tầng", "Trận thường", "Trận khó", "Cấp gợi ý"],
        [
            ["1–3", "120", "200", "1–4"],
            ["4–6", "220", "380", "4–7"],
            ["7–9", "350", "600", "7–10"],
            ["10–12", "500", "850", "10–13"],
            ["13–15", "700", "1200", "13–15"],
        ],
        "Bảng V.12 — Điểm từ ô hầm (sự kiện / nghỉ / cửa hàng = 0). Một đường khá ≈ 7000–7800 → cấp 15 ± 1.",
    )
    add_p(
        doc,
        "Điểm của một trận tăng theo từng cụm tầng, nên một trận ở tầng sâu cho nhiều điểm hơn một trận ở tầng đầu. "
        "Một đường đi ở mức khá, khoảng mười ô trận thường và trận khó, cho khoảng 7.000 đến 7.800 điểm. "
        "Khoảng điểm này sát với 7.210 điểm cần để đạt cấp 15. "
        "Vì vậy, nếu đi hầm một lượt và chọn đường hợp lý, đội sẽ tới boss ở quanh cấp mục tiêu, lệch khoảng một cấp. "
        "Người chơi không cần đi hết mọi ngõ, rồi quay lại đánh lại các tầng đã qua.",
    )
    add_table(
        doc,
        ["Cấp", "Máu Ren", "Máu Charlotte", "Máu Coda", "Tổng đội"],
        [
            ["1", "74", "140", "38", "252"],
            ["5", "86", "176", "48", "310"],
            ["10", "100", "218", "60", "378"],
            ["15", "114", "260", "73", "447"],
            ["18", "122", "284", "80", "486"],
        ],
        "Bảng V.13 — Máu cả đội khi cộng đúng (CHARACTER_LEVEL_PROGRESS.md)",
    )
    add_p(
        doc,
        "Máu của cả đội tăng đều qua các cấp, và không có một cấp nào máu tăng vọt so với cấp ngay trước. "
        "Tổng máu là 252 ở cấp 1, 447 ở cấp 15, và 486 ở cấp 18. "
        "Đoạn sau cấp 15 chỉ thêm 39 máu cho cả đội, vì ở đoạn này chỉ còn ba điểm cộng tay và ba lần tăng tự động. "
        "Phần lớn khả năng chịu đòn của đội đã có trước khi gặp boss. Mức nên có khi vào boss là khoảng cấp 15. Cấp 18 là mức đội đạt được sau khi thắng boss lần đầu.",
    )
    add_p(
        doc,
        "Tóm lại, việc nâng cấp trong lúc chơi chạy trên hai tiến trình riêng. "
        "Tiến trình trong trận bắt đầu khi đội thắng một ô có trận đánh trong hầm. Lúc đó cấp đội tăng, chỉ số tự tăng theo vai của từng người, mỗi người nhận một điểm cộng tay, và chiêu mới mở đúng mốc cấp. "
        "Tiến trình ở thành phố bắt đầu khi Ren học, làm thêm, nghỉ ngơi, trả lời câu hỏi trên lớp hoặc gặp người. Lúc đó hạng chỉ số đời sống tăng, và khi đủ điều kiện thì hạng gắn kết mới được mở. "
        "Hai tiến trình cùng cần thời gian ở buổi tối, và cùng bị giới hạn bởi hạn ngày 20/09. Điểm của tiến trình này không được cộng sang chỉ số của tiến trình kia.",
    )
    add_note(
        doc,
        "Số trong mục V.1 lấy từ CHARACTER_LEVEL_PROGRESS.md, combat-level-xp-progression-design, "
        "BOSS_ENCOUNTER_DESIGN.md, SKILL_KIT.md, DIFFICULTY.md và persona-calendar-design. "
        "Bậc khó trong mục này là Cadence.",
    )

    # --- V.2 social (trận đánh chuyển sang chương VI) ---
    add_heading_vi(doc, "V.2. Phân tích chỉ số đời sống", 2)
    add_p(
        doc,
        "Chỉ số đời sống là lớp thứ hai của chương này. Lớp chỉ số trận trả lời đội đánh mạnh đến đâu. "
        "Lớp chỉ số đời sống trả lời Ren được gặp ai, và mối gắn kết với người đó lên được đến hạng nào. "
        "Năm trục không cộng vào STR, Ma, HB hay EN.",
    )
    add_p(
        doc,
        "Năm trục gắn với lịch của Ren ở thành phố. Charlotte và Coda không có bộ trục riêng. "
        "Mỗi trục có hạng từ 1 đến 10. Làm một việc trong ngày chỉ cộng điểm kinh nghiệm của trục đó. "
        "Hạng tăng khi điểm tích lũy đạt ngưỡng của hạng kế tiếp, rồi số điểm vừa dùng để lên hạng được trừ đi. "
        "Một lần ghé shop hoa vì vậy không nhảy thẳng nhiều hạng.",
    )
    add_table(
        doc,
        ["Trục", "Vai trong đời sống", "Việc nuôi trục", "Gắn kết cần trục này"],
        [
            ["Resonance", "Mở quan hệ và sự thấu cảm", "Quán cà phê, shop hoa, câu hỏi giao tiếp", "Ren, Mei Lin, Astra"],
            ["Cadence", "Hiểu bài và mạch sự việc", "Thư viện, câu hỏi âm nhạc hoặc điều tra", "Coda, Ryo, Mei Lin"],
            ["Pulse", "Sự bền và việc luyện tập", "Luyện nhạc, câu hỏi thể chất", "Ren, Charlotte"],
            ["Harmony", "Việc làm chung và sự thành thạo", "Làm thêm, shop hoa, luyện nhạc", "Coda, Astra"],
            ["Rhythm", "Giữ nhịp và dám nghỉ", "Nghỉ buổi tối", "Charlotte, Ryo"],
        ],
        "Bảng V.31 — Năm trục đời sống và việc chúng mở (persona-calendar-design §5–§6, SocialStatsState)",
    )
    add_p(
        doc,
        "Resonance nuôi các cuộc gặp cần sự gần gũi. Cadence nuôi việc học và tuyến điều tra. "
        "Pulse nuôi việc luyện và những người cần sự kiên trì. Harmony nuôi việc làm thêm và người cần sự thành thạo. "
        "Rhythm gần như chỉ tăng khi Ren chọn nghỉ, nên trục này cạnh tranh trực tiếp với buổi tối vào hầm.",
    )
    add_p(
        doc,
        "Ngưỡng điểm để lên từng hạng, theo dữ liệu đang chạy trong game, là 15, 25, 40, 60, 85, 115, 150, 190 và 235. "
        "Hạng 1 lên hạng 2 cần 15 điểm. Hạng 9 lên hạng 10 cần 235 điểm. "
        "Cộng chín ngưỡng này lại, một trục cần 915 điểm để đi từ hạng 1 lên hạng 10. "
        "Phần lớn việc ban ngày cộng 8 điểm một lần. Với mức 8 điểm, một trục cần hơn một trăm lần làm đúng việc đó mới tới hạng 10. "
        "Trong khoảng từ đầu tháng 9 đến hạn Vault ngày 20/09, người chơi không đủ buổi để đẩy cả năm trục lên hạng cao cùng lúc.",
    )
    add_table(
        doc,
        ["Từ hạng", "Đến hạng", "Điểm cần", "Số lần làm việc +8 điểm"],
        [
            ["1", "2", "15", "2"],
            ["2", "3", "25", "4"],
            ["3", "4", "40", "5"],
            ["4", "5", "60", "8"],
            ["5", "6", "85", "11"],
            ["6", "7", "115", "15"],
            ["7", "8", "150", "19"],
            ["8", "9", "190", "24"],
            ["9", "10", "235", "30"],
        ],
        "Bảng V.32 — Ngưỡng hạng chỉ số đời sống (SocialStatsState). Số lần làm tròn lên theo mỗi lần +8 điểm.",
    )
    add_p(
        doc,
        "Buổi sáng không tốn lượt ban ngày. Trả lời đúng câu hỏi trên lớp được 8 điểm, theo chủ đề của câu. "
        "Câu về âm nhạc, lịch sử, logic hoặc điều tra cộng vào Cadence. Câu về giao tiếp hoặc văn học cộng vào Resonance. "
        "Câu về thể chất hoặc biểu diễn cộng vào Pulse. Trả lời sai thì không bị trừ điểm. "
        "Buổi sáng vì vậy là nguồn điểm đều, nhưng người chơi không chọn được trục. Trục nào tăng phụ thuộc vào câu của ngày đó.",
    )
    add_p(
        doc,
        "Ban ngày người chơi chọn một việc, và việc đó chọn trục được nuôi. "
        "Học ở thư viện cộng 8 điểm Cadence. Luyện nhạc cộng 8 điểm Harmony hoặc 8 điểm Pulse. "
        "Quán cà phê cộng 8 điểm Resonance. Làm ở cửa hàng tiện lợi cộng 10 điểm Harmony và 4 điểm Cadence. "
        "Shop hoa cộng 10 điểm Resonance và 4 điểm Harmony, và chỉ mở vào một số ngày trong tuần. "
        "Buổi tối cũng chỉ một việc. Nghỉ cộng 5 điểm Rhythm. Vào hầm thì buổi tối đó không cộng chỉ số đời sống, mà cộng điểm kinh nghiệm trận.",
    )
    add_p(
        doc,
        "Hạng đời sống là điều kiện để tăng hạng gắn kết, chưa phải là sức đánh. "
        "Muốn lên hạng gắn kết với một người, cần đủ điểm gắn kết, đủ hạng ở các trục người đó yêu cầu, và đôi khi cần thêm một mốc cốt truyện đã xảy ra. "
        "Ren cần Resonance và Pulse. Charlotte cần Pulse và Rhythm. Coda cần Cadence và Harmony. "
        "Astra cần Resonance và Harmony. Ryo cần Cadence và Rhythm. Mei Lin cần Cadence và Resonance. "
        "Khi đủ điều kiện, cảnh gặp được mở. Một số lần lên hạng gắn kết còn cho quyền lợi mang vào các lần vào hầm sau. Quyền lợi đó đi theo hạng gắn kết.",
    )
    add_table(
        doc,
        ["Người", "Trần hạng trong phần truyện đầu", "Điều kiện mở tiếp"],
        [
            ["Ren", "4", "Khoảng ngày 15"],
            ["Charlotte", "3", "Xong tuyến hầm thứ nhất"],
            ["Coda", "4", "Khoảng ngày 15"],
            ["Ryo", "2", "Phần truyện sau"],
            ["Mei Lin", "2", "Đã bắt đầu điều tra; hạng 3 trở đi thuộc phần sau"],
            ["Astra", "5", "Gặp được từ sau ngày 02/09"],
        ],
        "Bảng V.33 — Trần gắn kết trong phần truyện đầu (persona-calendar-design §6)",
    )
    add_p(
        doc,
        "Trần này có chủ đích. Dù Ren đã đủ điểm và đủ chỉ số đời sống, hạng gắn kết vẫn dừng ở mốc của phần truyện đầu cho đến khi cốt truyện mở khóa. "
        "Người chơi vẫn gặp được và vẫn nhận điểm gắn kết, nhưng hạng không tăng. Màn hình báo rằng cần tiến thêm câu chuyện. "
        "Vì vậy chỉ số đời sống mở cửa, còn cốt truyện quyết cửa đó được đi tiếp đến đâu.",
    )
    add_p(
        doc,
        "Đặt cạnh chỉ số trận, hai lớp trả lời hai lịch khác nhau. "
        "Chỉ số trận tăng khi thắng ô có trận trong hầm, và cả đội lên cùng một cấp. "
        "Chỉ số đời sống tăng khi dùng buổi sáng, ban ngày hoặc buổi tối ở thành phố, và từng trục lên hạng riêng. "
        "Từ ngày 7/09 đến ngày 20/09, buổi tối chỉ làm được một trong hai việc. "
        "Chọn hầm thì đội gần boss hơn. Chọn thành phố thì một trục đời sống hoặc một mối gắn kết nhích thêm. Cả hai không cộng số sang nhau.",
    )
    add_note(
        doc,
        "Ngưỡng 15 đến 235 lấy từ SocialStatsState. Bảng việc và trần gắn kết lấy từ persona-calendar-design. "
        "Tài liệu thiết kế cũ ghi mốc cuối là 120. Trong code, mốc để lên hạng 10 là 235.",
    )

    add_heading_vi(doc, "VI. Cơ chế trận đánh", 1)
    add_p(
        doc,
        "Phần này là cơ chế bên trong của một trận, nên đứng ở chương VI, không đứng trong chương chỉ số. "
        "Chương V chỉ nói đội mạnh lên bằng cách nào và chỉ số đời sống mở cửa gặp người bằng cách nào. "
        "Chương VI nói trong trận người chơi ra chiêu lúc nào.",
    )
    add_p(
        doc,
        "Cả trận bám một hàng thời gian. Hàng này chia ô. Mỗi ô là một nhịp của bài hát. "
        "Nhạc chạy ngay khi vào trận. Không còn màn “xếp đội rồi mới bật nhạc” như bản thiết kế cũ.",
    )
    add_p(
        doc,
        "Bản GDD cũ ghi: xếp đội → dừng ở nhịp 6 → chạy hai khúc, mỗi khúc 32 nhịp. "
        "Bản đang chơi không còn vậy. Vòng đúng như dưới đây.",
    )

    add_heading_vi(doc, "VI.1. Một trận diễn ra thế nào", 3)
    add_table(
        doc,
        ["Khâu", "Hàng thời gian", "Nhạc"],
        [
            ["Vào trận (một lần)", "Chạy 12 nhịp cho khớp bài. Chưa tính hiệp. Chưa hiện đòn địch.", "To"],
            ["Cửa nghĩ", "Hàng đứng. Kéo đội, gắn chiêu, bấm Cover nếu đủ.", "Vẫn chạy, nhỏ hơn"],
            ["Cửa chạy", "Quét đủ 22 nhịp (một hiệp). Đòn, đỡ, phản đòn tính đúng ô.", "To lại"],
        ],
        "Bảng V.14 — Ba khâu một trận (COMBAT_MECHANICS.md §1). [Hình 81–86: sửa chú thích, bỏ Deploy / 32 nhịp]",
    )
    add_p(
        doc,
        "Lúc vào (một lần). Hàng thời gian chạy 12 nhịp cho khớp bài. Chưa tính hiệp. Chưa hiện đòn địch. "
        "Hết 12 nhịp thì mở cửa nghĩ.",
    )
    add_p(
        doc,
        "Cửa nghĩ. Hàng thời gian đứng. Nhạc vẫn chạy, nhỏ hơn. Lúc này làm cùng lúc được: kéo người trên lưới, "
        "gắn chiêu lên hàng của từng người, bấm Cover nếu đủ năng lượng. Chỉ có một nút để chạy trận. "
        "Gắn hết chiêu không tự đánh.",
    )
    add_p(
        doc,
        "Cửa chạy. Nhạc to lại. Hàng thời gian quét đủ 22 nhịp (một hiệp). Đòn, đỡ, phản đòn tính đúng ô đang quét. "
        "Hết 22 nhịp thì lại mở cửa nghĩ. Hàng không nhảy về đầu bài.",
    )
    add_p(
        doc,
        "Hết bài (677 nhịp) thì hết trận. Bài boss đang dùng: 152 nhịp mỗi phút, khoảng 30 hiệp.",
    )
    add_p(
        doc,
        "Vì sao có cửa nghĩ rồi mới chạy. Game vẫn là xếp đội. Người chơi cần lúc dừng để xem: đứng đâu, chiêu nào, "
        "đòn địch ở ô nào. Khi hàng chạy mới phải bấm đúng nhịp. Nghĩ lúc nhạc nhỏ. Đánh lúc nhạc to.",
    )

    add_heading_vi(doc, "VI.2. Sân và độ dài chiêu", 3)
    add_p(
        doc,
        "Sân có hai bên. Mỗi bên 2 hàng × 3 cột. Đội mình tối đa 4 người. Địch tối đa 6. "
        "Lúc nghĩ, ô bên mình hiện. Lúc chạy, ô ẩn. Ô địch không hiện: không kéo được quái.",
    )
    add_note(doc, "[Hình 87 — Sàn hai bên, 2 hàng × 3 cột]")
    add_p(doc, "Mỗi chiêu chiếm ba khúc trên hàng thời gian.", first_line=False)
    add_p(doc, "Khúc trước: đứng lấy đà.", first_line=False)
    add_p(doc, "Khúc giữa: ra đòn hoặc phản đòn.", first_line=False)
    add_p(doc, "Khúc sau: đứng hồi.", first_line=False)
    add_table(
        doc,
        ["Ô nhịp", "8", "9", "10", "11", "12", "13"],
        [
            ["Khúc", "Lấy đà", "Lấy đà", "Ra đòn", "Ra đòn", "Hồi", "Hồi"],
            ["Phản đòn", "—", "—", "trùng", "trùng", "—", "—"],
        ],
        "Bảng V.15 — Ví dụ chiêu giữa của Ren (2-2-2) đặt từ ô 8 (SKILL_KIT.md). [Hình 88–89]",
    )
    add_p(
        doc,
        "Một người không được chồng hai chiêu lên cùng ô. Không còn “điểm lượt” để giới hạn số chiêu. "
        "Cái giới hạn là chỗ trống trên hàng của người đó. Chiêu thường ngắn. Chiêu mạnh dài hơn, để phủ nhiều đòn địch. "
        "Ba khúc để không gắn chiêu sát nút hết hàng.",
    )
    add_p(
        doc,
        "Chỗ đứng quyết ai bị đánh. Quái thích đánh hàng trước. Không phản được đòn thì một người phải ăn: "
        "ưu tiên người đang đứng đúng ô đó; không ai đứng thì người nhanh nhất trong đội còn sống ăn thay. "
        "Kéo người đỡ lên trước là việc thật, không phải cho đẹp.",
    )

    add_heading_vi(doc, "VI.3. Đỡ, phản đòn, bấm đúng lúc", 3)
    add_p(
        doc,
        "Không còn chiêu “đỡ” trong bộ ba chiêu. Lúc hàng đang chạy, bấm Space để đặt một tấm chắn đúng ô.",
    )
    add_table(
        doc,
        ["Lúc đặt chắn so với ô đòn địch", "Giảm sát thương"],
        [
            ["Đúng ô", "68%"],
            ["Sớm một ô", "25%"],
            ["Muộn một ô", "10%"],
            ["Lệch hơn một ô", "0%"],
        ],
        "Bảng V.16 — Đỡ bằng Space (COMBAT_MECHANICS.md). Off Beat trừ thêm 10% ở cửa sớm / muộn. [Hình 90]",
    )
    add_p(
        doc,
        "Mỗi hiệp chỉ đỡ có hiệu lực tối đa 7 lần. Ô đã phản đòn rồi thì không cần đỡ.",
    )
    add_p(
        doc,
        "Phản đòn: gắn chiêu sao cho khúc giữa trùng ô đòn địch. Đủ lần thì đòn địch bị hủy. Thiếu thì đòn vẫn đánh vào đội.",
    )
    add_table(
        doc,
        ["Màu nốt", "Số lần trùng để phá"],
        [
            ["Đỏ", "1"],
            ["Xanh", "2"],
            ["Tím", "3"],
        ],
        "Bảng V.17 — Màu nốt địch (COMBAT_MECHANICS.md). [Hình 93]",
    )
    add_p(
        doc,
        "Đôi khi, đúng lúc đang phản, hiện một vòng trên màn. Bấm Space (hoặc bấm chuột) khi vòng khớp khung.",
    )
    add_table(
        doc,
        ["", "Khớp đẹp", "Khớp vừa", "Trượt", "Không hiện vòng"],
        [
            ["Hủy đòn địch", "Có", "Có", "Không (đòn vẫn đánh)", "Như phản đủ lần"],
            ["Sát thương mình", "×1,50", "×1,20", "Giảm theo hiệp", "×1,0"],
        ],
        "Bảng V.18 — Vòng bấm đúng lúc khi đang phản đòn (COMBAT_MECHANICS.md QTE)",
    )
    add_p(
        doc,
        "Nhiều người trùng một ô: chỉ một vòng, theo người có nhịp tim cao hơn. "
        "Thứ tự tính: phản đủ thì hủy đòn → chưa đủ thì đỡ nếu đúng ô → không được thì trừ máu. "
        "Người chơi luôn có việc: đọc đòn, gắn chiêu, hoặc giơ chắn.",
    )

    add_heading_vi(doc, "VI.4. Hai thanh năng lượng trong trận", 3)
    add_table(
        doc,
        ["", "Prep (từng người)", "Cover (cả đội)"],
        [
            ["Tối đa", "3 vạch", "10 vạch"],
            ["Cộng khi nào", "Để trống khúc giữa chiêu mạnh", "Cùng điều kiện"],
            ["Không cộng", "Chiêu thường · trùng đòn địch", "Chiêu thường · trùng đòn địch"],
            ["Dùng lúc nào", "Chiêu mạnh tốn 1 · chiêu cuối tốn 2", "Cửa nghĩ, đủ 8, Ren còn sống: bấm Cover, mất 8"],
            ["Hiệu quả", "Chiêu đó mạnh hơn", "12 nhịp sau: cả đội đánh ×1,25"],
        ],
        "Bảng V.19 — Prep và Cover (SKILL_KIT.md). [Hình 91 Prep · Hình 92 Cover]",
    )
    add_p(
        doc,
        "Vì sao hai thanh. Prep là từng người chuẩn bị. Cover là cả đội bùng một nhịp. "
        "Gộp một thanh thì chỉ Ren được việc. Hai người kia mất lý do để trống nhịp.",
    )

    add_heading_vi(doc, "VI.5. Ba chiêu mỗi người", 3)
    add_p(doc, "Mỗi người chỉ có ba chiêu. Phím W, A, D.")
    add_table(
        doc,
        ["Người", "Chiêu thường", "Chiêu giữa", "Chiêu mạnh"],
        [
            ["Ren", "Strike 1-1-1", "Crosscut 2-2-2 (phủ hai ô)", "Finale 2-3-3 (phá nốt tím)"],
            ["Charlotte", "Ram 1-1-1", "Anchor 2-2-2 (đẩy đòn địch lùi)", "Bulwark 2-2-3 (khiên)"],
            ["Coda", "Pulse 1-1-1", "Mend 2-1-2 (hồi máu)", "Encore 1-1-1 (chiêu sau của đồng đội ngắn hơn)"],
        ],
        "Bảng V.20 — Bộ ba chiêu (SKILL_KIT.md). [Hình 95 Coda · Hình 96 Charlotte · Hình 97 Ren]",
    )
    add_p(
        doc,
        "Ren xóa đòn trên hàng. Charlotte mua thời gian và đứng trước. Coda giữ đội sống. "
        "Ba người làm ba việc khác nhau. Không cần chiêu thứ tư.",
    )

    add_heading_vi(doc, "VI.6. Cái gì đã chạy trong Unity", 3)
    add_table(
        doc,
        ["Hạng mục", "Trạng thái"],
        [
            ["Sân 2 × 3, kéo đội mỗi cửa nghĩ", "Đã có"],
            ["Cửa nghĩ / cửa chạy, nhạc chạy suốt, 22 nhịp một hiệp", "Đã có"],
            ["Prep, Cover, nốt đỏ / xanh / tím", "Đã có"],
            ["Đỡ Space, vòng bấm đúng lúc", "Đã có"],
            ["Ba độ khó On Beat / Cadence / Off Beat", "Đã có"],
            ["Giới hạn điểm lượt mỗi hiệp", "Đã bỏ"],
            ["Chiêu đỡ trong bộ ba chiêu", "Đã bỏ"],
            ["Màn xếp đội tách nhạc · dừng bài ở nhịp 6", "Đã bỏ"],
            ["Một số nút boss (xoay hệ, chọn đồng đội lúc hồi)", "Chưa xong hết"],
        ],
        "Bảng V.21 — Prototype Unity (khớp COMBAT_MECHANICS.md, không còn Deploy / 32 nhịp)",
    )

    # --- V.3 ---
    add_heading_vi(doc, "V.3. Bản đồ hầm", 2)
    add_p(
        doc,
        "Hầm mở buổi tối, sau khi đã chọn việc trên lịch. Xong hoặc thoát thì về thành phố. Lịch không bị bản đồ thay.",
    )
    add_p(
        doc,
        "Máy sinh sẵn một mạng ô từ một mã số. Bảy cột. Mười lăm tầng đánh. Boss ở tầng 16. Có nhiều nhánh. "
        "Ô không nối tới đường đi thì bị xóa. Một số tầng cố định (đầu, giữa, trước boss). "
        "Còn lại gieo ngẫu nhiên. Luôn có đường lên boss.",
    )
    add_note(doc, "[Hình 102–108 — Khung map → sáu đường → xóa ô thừa → gán việc → thêm tầng boss]")
    add_p(doc, "Mỗi tầng chỉ chọn một ô đang nối với chỗ đang đứng.")
    add_table(
        doc,
        ["Loại ô", "Việc"],
        [
            ["Trận thường", "Đánh, được điểm cấp đội"],
            ["Trận khó", "Đánh nặng hơn, được nhiều điểm hơn"],
            ["Sự kiện", "Đọc, chọn, mở cờ truyện"],
            ["Nghỉ", "Hồi"],
            ["Cửa hàng", "Mua"],
            ["Rương", "Nhận đồ"],
            ["Boss", "Thủ lĩnh của tuyến đó"],
        ],
        "Bảng V.22 — Loại ô trên bản đồ hầm",
    )
    add_table(
        doc,
        ["Tuyến", "Boss cuối"],
        [
            ["Pulse", "Mimi — The Pulse"],
            ["Echo", "Kiki — The Echo"],
            ["Canticle", "Astra — Chart Lord"],
        ],
        "Bảng V.23 — Ba tuyến hầm đầu (Pinky Vault). Điểm từng ô xem Bảng V.12, không dán lại.",
    )
    add_p(
        doc,
        "Vì sao phải chọn đường, không đi một lối thẳng. Hầm có hạn ngày. Phải quyết: đánh khó để lên cấp, "
        "nghỉ cho sống, hay lấy sự kiện. Bản đồ cho chỗ chọn. Lịch cho biết còn bao nhiêu tối để chọn.",
    )

    # --- V.4 ---
    add_heading_vi(doc, "V.4. Lịch thành phố và chuyện gắn kết", 2)
    add_p(doc, "Ngày thường có ba nhịp.")
    add_table(
        doc,
        ["Nhịp", "Mất lượt?", "Việc"],
        [
            ["Sáng", "Không", "Câu hỏi trên lớp. Đúng thì cộng chỉ số đời sống."],
            ["Ban ngày", "Có (một việc)", "Học, làm thêm, gặp người, rèn chỉ số đời sống."],
            ["Buổi tối", "Có (một việc)", "Gặp người, nghỉ, hoặc vào hầm."],
        ],
        "Bảng V.24 — Một ngày (persona-calendar-design §4)",
    )
    add_table(
        doc,
        ["Việc", "Điểm đời sống"],
        [
            ["Học (thư viện)", "Cadence +8"],
            ["Luyện nhạc", "Harmony +8 hoặc Pulse +8"],
            ["Nghỉ", "Rhythm +5"],
            ["Làm cửa hàng tiện lợi", "Harmony +10, Cadence +4"],
            ["Shop hoa", "Resonance +10, Harmony +4"],
            ["Câu hỏi lớp (đúng)", "+8 theo chủ đề câu"],
        ],
        "Bảng V.25 — Việc nào cộng trục nào (persona-calendar-design §5)",
    )
    add_p(
        doc,
        "Gặp người để mở chuyện gắn kết. Mỗi người một khóa riêng. "
        "Đủ điểm gắn kết, đủ chỉ số đời sống, đôi khi đủ cờ truyện, thì lên hạng. "
        "Hạng mở đoạn kể và hiệu ứng phụ trong trận. Gặp người không làm đội lên cấp đánh nhau.",
    )
    add_table(
        doc,
        ["Người", "Khóa", "Chỉ số cần"],
        [
            ["Ren", "Melody", "Resonance, Pulse"],
            ["Charlotte", "Bass", "Pulse, Rhythm"],
            ["Coda", "Harmony", "Cadence, Harmony"],
            ["Ryo", "Measure", "Cadence, Rhythm"],
            ["Mei Lin", "Dissonance", "Cadence, Resonance"],
            ["Astra", "Pulse", "Resonance, Harmony"],
        ],
        "Bảng V.26 — Khóa gắn kết (persona-calendar-design §6)",
    )
    add_table(
        doc,
        ["Khóa", "Trần phần truyện đầu", "Mở tiếp khi"],
        [
            ["Ren (Melody)", "Hạng 4", "Khoảng ngày 15"],
            ["Charlotte (Bass)", "Hạng 3", "Xong tuyến hầm 1"],
            ["Coda (Harmony)", "Hạng 4", "Khoảng ngày 15"],
            ["Ryo (Measure)", "Hạng 2", "Phần truyện sau"],
            ["Mei Lin (Dissonance)", "Hạng 2", "Bắt đầu điều tra; hạng 3+ phần sau"],
            ["Astra (Pulse)", "Hạng 5", "Gặp từ sau ngày 02/09"],
        ],
        "Bảng V.27 — Trần hạng gắn kết trong phần truyện đầu",
    )
    add_p(
        doc,
        "Đoạn kể (hình ảnh + thoại) chạy trên lịch: lễ, gặp lại, nhận hạn vào hầm. Đọc xong, ngày trôi. "
        "Vì sao lịch ngồi trên bản đồ hầm. Mục tiêu dài là ngày 20/09. Nếu vào hầm lúc nào cũng được, "
        "hạn chỉ còn một dòng chữ. Mỗi tối chỉ một việc thì vào hầm là một cái giá.",
    )

    # --- V.5 ---
    add_heading_vi(doc, "V.5. Quái trong hầm nhạc Pop (phần truyện đầu)", 2)
    add_p(
        doc,
        "Phần đầu lấy nhạc Pop của nhóm đứng đầu bảng. Quái phải đọc được trên hàng thời gian, không chỉ là túi máu.",
    )
    add_table(
        doc,
        ["Lớp", "Nốt", "Việc dạy"],
        [
            ["Quái thường", "Đỏ (một lần trùng)", "Nhìn một ô"],
            ["Quái mạnh", "Chủ yếu đỏ, khoảng 30% xanh", "Phủ hai ô, hoặc hai người cùng ô"],
            ["Trận mở đầu (05/09)", "Ít nốt, rõ", "Cửa nghĩ rồi cửa chạy, trước khi mở hầm thật"],
            ["Boss cuối tuyến", "Đỏ / xanh / tím trên cùng hàng", "Vừa xóa nốt vừa giữ đội, lịch vẫn đếm ngày"],
        ],
        "Bảng V.28 — Bốn lớp quái, cùng một hàng thời gian",
    )
    add_p(
        doc,
        "Boss cuối tuyến có ba phần. Thân chính phải hạ mới thắng. Hai phần phụ phá nhịp, không phải điều kiện thắng. "
        "Hệ nhân sát thương chỉ vào thân chính.",
    )
    add_table(
        doc,
        ["Phần", "Máu", "Việc"],
        [
            ["Thân chính (CORE)", "1680", "Hạ hết thì thắng"],
            ["Phần phụ MICRO", "280", "Phá nhịp; không cần hạ để thắng"],
            ["Phần phụ EYE", "200", "Phá nhịp; không cần hạ để thắng"],
        ],
        "Bảng V.29 — Ba phần boss (BOSS_ENCOUNTER_DESIGN.md)",
    )
    add_table(
        doc,
        ["Màu nốt boss", "Đòn boss đau thêm"],
        [
            ["Tím", "×1,35"],
            ["Xanh", "×1,15"],
            ["Đỏ", "×1,00"],
        ],
        "Bảng V.30 — Nốt càng khó phá thì đòn càng đau nếu để lọt",
    )
    add_p(
        doc,
        "Vì sao ba lớp. Thường dạy một nốt. Mạnh dạy đủ lần trùng. Boss dạy vừa xóa nốt vừa giữ đội, "
        "trong khi lịch vẫn đếm ngày. Độ khó không thêm loài quái. Chỉ đổi máu, đòn, và level quái.",
    )

    add_heading_vi(doc, "Năm thành phần, một câu mỗi thứ", 2)
    add_p(doc, "Chỉ số tách trận và đời sống, nên mỗi ngày phải chọn.", first_line=False)
    add_p(doc, "Chỉ số đời sống mở cửa gặp người. Hạng càng cao thì càng tốn điểm.", first_line=False)
    add_p(doc, "Trận, ở chương VI: nghĩ khi hàng đứng, đánh khi hàng chạy.", first_line=False)
    add_p(doc, "Bản đồ hầm: chọn lối, không đi thẳng.", first_line=False)
    add_p(doc, "Lịch: mỗi tối một việc, vào hầm phải trả giá.", first_line=False)
    add_p(doc, "Quái: là nốt trên hàng, không phải túi máu đứng im.", first_line=False)

    add_heading_vi(doc, "Phụ lục — Bảng cấp đầy đủ từng người", 1)
    add_note(doc, "Các bảng dài Lv1–18. Trong thân bài chỉ cần Bảng V.5, V.8 và V.13. Phụ lục để đối chiếu.")

    add_p(doc, "Ren — đánh chính, hệ Giai điệu. Máu = STR × 2 + 30.", first_line=False)
    add_table(
        doc,
        ["Lv", "STR", "Ma", "HB", "EN", "HP", "W", "Chiêu mở"],
        [
            ["1", "22", "6", "145", "4", "74", "7", "Strike"],
            ["4", "27", "6,6", "146,5", "5,6", "84", "8", "Crosscut"],
            ["10", "35", "7,8", "159,5", "8,8", "100", "8", "Finale"],
            ["15", "42", "8,8", "167", "11,8", "114", "8", ""],
            ["18", "46", "9,4", "173,5", "13,4", "122", "9", ""],
        ],
        "Bảng P.1 — Ren, các mốc chính (đủ Lv1–18 trong CHARACTER_LEVEL_PROGRESS.md)",
    )
    add_p(doc, "Charlotte — đỡ đòn, hệ Nhịp. Máu = STR × 6 + 50.", first_line=False)
    add_table(
        doc,
        ["Lv", "STR", "Ma", "HB", "EN", "HP", "W", "Chiêu mở"],
        [
            ["1", "15", "5", "105", "10", "140", "7", "Ram"],
            ["3", "18", "5,2", "106", "11,6", "158", "7", "Anchor"],
            ["9", "27", "5,8", "114", "15,4", "212", "7", "Bulwark"],
            ["15", "35", "6,4", "127", "19,2", "260", "7", ""],
            ["18", "39", "6,7", "133,5", "21,1", "284", "7", ""],
        ],
        "Bảng P.2 — Charlotte, các mốc chính",
    )
    add_p(doc, "Coda — hỗ trợ, hệ Hòa âm. Máu = STR × 2 + Ma × 0,35 + 15.", first_line=False)
    add_table(
        doc,
        ["Lv", "STR", "Ma", "HB", "EN", "HP", "W", "Chiêu mở"],
        [
            ["1", "6", "30", "125", "3", "38", "7", "Pulse"],
            ["5", "10", "36", "127", "5,8", "48", "7", "Mend"],
            ["11", "16", "44", "140", "9", "62", "7", "Encore"],
            ["15", "20", "50", "147", "10,8", "73", "8", ""],
            ["18", "23", "54", "153,5", "12,4", "80", "8", ""],
        ],
        "Bảng P.3 — Coda, các mốc chính",
    )

    out_dir = Path(r"F:\Unity_Project\Fractured Chorus\docs\gdd")
    out_dir.mkdir(parents=True, exist_ok=True)
    out_path = out_dir / "Fractured-Chorus-GDD-Phan-V-He-thong-trong-game.docx"
    try:
        doc.save(out_path)
    except PermissionError:
        out_path = out_dir / "Fractured-Chorus-GDD-Phan-V-He-thong-trong-game-cap-nhat.docx"
        doc.save(out_path)

    downloads = Path(r"c:\Users\admin\Downloads") / out_path.name
    try:
        doc.save(downloads)
    except PermissionError:
        downloads = Path(r"c:\Users\admin\Downloads") / "Fractured-Chorus-GDD-Phan-V-He-thong-trong-game-cap-nhat.docx"
        doc.save(downloads)
    print(out_path)
    print(downloads)


if __name__ == "__main__":
    build()
