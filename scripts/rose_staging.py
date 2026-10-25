"""Authored staging for the four authoritative scripts; anchors are checked exactly once.

Cast lists describe visible physical presence, independently of who speaks or narrates.
An absent caller never appears in the room. Ordinary stage backgrounds end a CG.
"""

SCRIPT_CG = {"纱CG01": "cg01", "纱CG02": "cg02", "纱CG03A": "cg03a", "纱CG03B": "cg03b",
      "纱CG04": "cg04", "纱CG05": "cg05", "纱CG06": "cg06", "纱CG07": "cg07",
      "纱CG08": "cg08", "纱CG09": "cg09", "纱CG10A": "cg10a", "纱CG10B": "cg10b", "纱CG10C": "cg10c"}
SUPPLEMENTAL_CG = {"纱CG11": "cg11", "纱CG12": "cg12", "纱CG13": "cg13", "纱CG14": "cg14", "纱CG15": "cg15", "纱CG16": "cg16",
                   "纱CG17": "cg17", "纱CG18": "cg18", "纱CG19": "cg19", "纱CG20": "cg20", "纱CG21": "cg21",
                   "纱CG22": "cg22", "纱CG23": "cg23", "纱CG24": "cg24", "纱CG25": "cg25", "纱CG26": "cg26", "纱CG27": "cg27",
                   "纱CG28": "cg28", "纱CG29": "cg29", "纱CG30": "cg30", "纱CG31": "cg31", "纱CG32": "cg32", "纱CG33": "cg33"}
CG = SCRIPT_CG | SUPPLEMENTAL_CG
YE = ["ye-zhao"]
TANG = ["tang-jianyue"]
BOTH = ["ye-zhao", "tang-jianyue"]
EMPTY_TANG = ["tang-jianyue-empty"]
BOTH_EMPTY = ["ye-zhao", "tang-jianyue-empty"]
PHOTO_CAST = ["ye-zhao-home", "tang-jianyue-empty"]
WORK = ["ye-zhao-work", "tang-jianyue-work"]
HOME = ["ye-zhao-home", "tang-jianyue-home"]
MEASURING = ["ye-zhao", "tang-jianyue-measuring"]
ZHOU = "zhou-suqin"
ZHOU_LISTENING = "zhou-suqin-listening"
ZHOU_EMPTY = "zhou-suqin-empty-handed"
ZHOU_LISTENING_EMPTY = "zhou-suqin-listening-empty"
AUNT = "tang-yufen"
AUNT_STERN = "tang-yufen-stern"
TANG_HOME = ["tang-jianyue-home"]
TANG_SORE_WRIST = ["tang-jianyue-home-sore-wrist"]
WASHING = ["ye-zhao-washing", "tang-jianyue-home"]
WASHING_QUIET = ["ye-zhao-washing-thoughtful", "tang-jianyue-home"]
TEARFUL_HOME = ["ye-zhao-home-crying", "tang-jianyue-home-tearful"]
PHONE = ["ye-zhao-residency-phone"]
PHONE_TENSE = ["ye-zhao-residency-phone-tense"]
PHONE_CHARGING = ["ye-zhao-residency-phone-charging"]
REUNION_CARRYING = ["ye-zhao-reunion-carrying", "tang-jianyue-reunion"]
REUNION_WEARING = ["ye-zhao-reunion-wearing", "tang-jianyue-reunion"]
LATE_TANG = ["tang-jianyue-late-cardigan"]
NIGHT_BACKGROUNDS = {"bg03": "bg03-night", "bg04": "bg04-night", "bg06": "bg06-night", "bg07": "bg07-night", "bg07-sorted": "bg07-sorted-night", "bg09": "bg09-night", "bg07-repaired": "bg07-repaired-night", "bg06-temporary": "bg06-temporary-night", "bg09-winter": "bg09-winter-night", "bg09-spring": "bg09-spring-night", "bg09-summer": "bg09-summer-night", "bg11": "bg11-night"}
NIGHT_BACKGROUNDS["bg04-winter"] = "bg04-winter-night"
NIGHT_BACKGROUNDS["bg07-sorted-winter"] = "bg07-sorted-winter-night"
NIGHT_BACKGROUNDS["bg05-winter"] = "bg05-winter-night"
NIGHT_BACKGROUNDS["bg06-winter"] = "bg06-winter-night"
REFLECTIVE = {"纱S06", "纱S07", "纱S08", "纱S09", "纱S10", "A01", "A02", "A04", "A05", "A05a",
              "B04", "B06", "B07", "B07a", "C01", "C02", "C04", "C05", "C05a", "C06", "C07", "C08"}

# Section id: initial background, player-facing date, physical cast, lighting.
SECTIONS = {
    "纱S01": ("bg02-hallway", "2029年1月18日 · 15:26", ["ye-zhao-work", "luo-xiaoan"], "day"),
    "纱S02": ("bg02", "2029年1月18日 · 婚礼前", WORK + ["he-qing-worried"], "day"),
    "纱S03": ("bg03", "2029年2月至3月", BOTH, "day"),
    "纱S04": ("bg04", "2029年4月 · 午后", MEASURING, "rain"),
    "纱S05": ("bg03", "2029年5月", BOTH, "day"),
    "纱S06": ("bg07", "2029年7月", HOME, "day"),
    "纱S07": ("bg06", "2029年9月上旬 · 暴雨夜", YE, "rain-night"),
    "纱S08": ("bg04", "2029年9月27日 · 夜", BOTH_EMPTY, "cool"),
    "纱S09": ("bg09", "2029年10月 · 驻留第一周", YE, "day"),
    "纱S10": ("bg09", "2029年11月26日 · 午后", YE, "day"),
    "A01": ("bg09", "2029年11月26日 · 夜", YE, "night"),
    "A02": ("bg04", "2029年12月5日 · 傍晚", BOTH, "day"),
    "A03": ("bg07", "2029年12月6日 · 清晨", HOME, "day"),
    "A04": ("bg07-january-leak", "2030年1月上旬", HOME, "rain"),
    "A05": ("bg03", "2030年1月下旬", BOTH, "day"),
    "A05a": ("bg06", "2030年1月末", YE, "night"),
    "A06": ("bg08", "2030年2月中旬", BOTH_EMPTY, "day"),
    "A07": ("bg03", "2030年春", BOTH, "day"),
    "B01": ("bg09", "2029年11月26日 · 夜", YE, "night"),
    "B02": ("bg03", "2029年12月5日", BOTH, "day"),
    "B03": ("bg09", "2029年12月下旬 · 探访第一天", ["ye-zhao", "tang-jianyue-travel"], "day"),
    "B04": ("bg09", "2030年1月", YE, "night"),
    "B05": ("bg07", "2030年2月上旬 · 试住", HOME, "day"),
    "B06": ("bg07", "2030年2月中旬 · 深夜", HOME, "night"),
    "B07": ("bg07", "2030年2月 · 离开当日", HOME, "day"),
    "B07a": ("bg09", "2030年春末 · 22:30", YE, "night"),
    "B08": ("bg03-reunion", "2033年冬 · 约定重逢", REUNION_CARRYING, "day"),
    "C01": ("bg09", "2029年11月26日 · 夜", YE, "night"),
    "C02": ("bg09", "2029年12月", YE, "day"),
    "C03": ("bg03", "2030年1月下旬", BOTH, "day"),
    "C04": ("bg09", "2030年2月", YE, "day"),
    "C05": ("bg09", "2030年6月12日 · 开幕日", YE, "day"),
    "C05a": ("bg03-ironing", "2030年6月末", EMPTY_TANG, "day"),
    "C06": ("bg09", "2030年8月 · 夜", YE, "night"),
    "C07": ("bg11", "2031年春 · 搬家后", YE, "day"),
    "C08": ("bg04", "2033年冬 · 午后", LATE_TANG, "day"),
}


def cue(section, anchor, background=None, cast=None, time=None, tone=None, music=None, mood=None, illustration=None, room_sorted=None, room_repaired=None):
    return dict(section=section, anchor=anchor, background=background, cast=cast,
                time=time, tone=tone, music=music, mood=mood, illustration=illustration, room_sorted=room_sorted, room_repaired=room_repaired)


CUES = [
    cue("纱S01", "我侧身挤进储物间", "bg01", WORK),
    cue("纱S01", "门外灯光涌进", "bg02-hallway", WORK + ["pan-cheng"], tone="day"),
    cue("纱S01", "那个女人终于笑出声", "bg02", WORK),
    cue("纱S02", "贺晴：那我是不是该请你吃饭", "bg02", WORK + ["he-qing-relieved"]),
    cue("纱S02", "新人终于进场", "bg02-hallway", WORK),
    cue("纱S02", "后来我整理照片", "bg06", ["ye-zhao-work"], time="2029年1月18日 · 夜", tone="night"),
    cue("纱S02", "接衣坊门铃", "bg03", BOTH, time="2029年1月19日 · 16:10", tone="day"),
    cue("纱S03", "从缝扣子的针接筷子落碗", "bg07", BOTH),
    cue("纱S03", "饭后我抢着洗碗", cast=WASHING),
    cue("纱S03", "转工作间", "bg04", BOTH),
    cue("纱S04", "外套先留我这儿", "bg04", MEASURING),
    cue("纱S04", "她上楼拿布", "bg07", BOTH, mood="thoughtful"),
    cue("纱S04", "声音延长至叶照工作室", "bg06", YE, tone="night"),
    cue("纱S04", "同一天，衣坊收店后", "bg04", TANG, time="2029年4月 · 同日收店后", tone="night"),
    cue("纱S05", "白日热浪转晚间虫鸣", "bg03", BOTH, time="2029年6月 · 停电前夕", tone="day"),
    cue("纱S05", "我带了风扇、冰西瓜", time="2029年6月 · 次日傍晚"),
    cue("纱S05", "店里热得站不住", "bg05-day", ["ye-zhao", "tang-jianyue-handsewing"]),
    cue("纱S05", "她教了我两针", illustration="纱CG12"),
    cue("纱S05", "她捏住我拿衣服的手", "bg05", ["ye-zhao", "tang-jianyue-shy"], time="2029年6月 · 入夜", tone="day"),
    cue("纱S05", "稍晚，纱BG07", "bg07", HOME, tone="night"),
    cue("纱S05", "我把汤喝完，洗了碗", cast=WASHING),
    cue("纱S06", "唐玉芬来的那天，我在厨房切黄瓜", mood="warm", illustration="纱CG18"),
    cue("纱S06", "唐玉芬：见月，我给你拿了点", "bg07", HOME + [AUNT]),
    cue("纱S06", "姨妈停在楼梯口", cast=HOME + [AUNT_STERN], mood="thoughtful"),
    cue("纱S06", "下楼后替客人倒水", "bg03", YE),
    cue("纱S06", "次日纱BG06", "bg06", YE, time="2029年7月 · 次日"),
    cue("纱S06", "八月午后", "bg03", BOTH_EMPTY + [ZHOU], time="2029年8月 · 午后", tone="day"),
    cue("纱S06", "我走到门外", "bg05-day", YE),
    cue("纱S06", "周素琴走时", "bg05-day", BOTH_EMPTY + [ZHOU_LISTENING], time="2029年8月 · 傍晚"),
    cue("纱S06", "唐见月：你一直在？", cast=BOTH_EMPTY),
    cue("纱S06", "纱BG07。剩饭重新加热", "bg07", HOME, tone="night"),
    cue("纱S07", "我拿着背景布过去", "bg07-rain-night", HOME, tone="rain-night-painted"),
    cue("纱S07", "快天亮时", "bg07-rain-dawn", time="2029年9月上旬 · 雨后清晨", tone="rain"),
    cue("纱S07", "雨声淡入打印机声", "bg06", YE, time="2029年9月 · 午后", tone="day"),
    cue("纱S07", "晚上我把日历给她看", "bg07", HOME, time="2029年9月 · 夜", tone="night"),
    cue("纱S07", "几年前的记忆", "bg06", YE, time="几年前 · 叶照的回忆", tone="memory"),
    cue("纱S07", "手机震动，回到九月", "bg06", YE, time="2029年9月 · 现在", tone="day"),
    cue("纱S07", "纱BG08空房", "bg08", BOTH_EMPTY + ["liu-qing"], tone="day", mood="warm"),
    cue("纱S09", "秦嘉：你这组先留着", "bg09-work-area", ["ye-zhao-residency", "qin-jia-thinking"], time="2029年10月 · 作品讨论", tone="day"),
    cue("纱S09", "秦嘉：那就先不用按原来拍", cast=["ye-zhao-residency", "qin-jia"]),
    cue("纱S09", "第二周晚上，我收到见月的消息", "bg09", YE, time="2029年10月 · 第二周夜间", tone="night"),
    cue("纱S08", "收起合同，去拿主相机", "bg04", BOTH_EMPTY),
    cue("纱S09", "雁汀，十月末", "bg07", TANG_SORE_WRIST, time="2029年10月末 · 雁汀", tone="day"),
    cue("纱S09", "姨妈来了，说过阵子", cast=TANG_SORE_WRIST + [AUNT_STERN]),
    cue("纱S09", "她气得说了些话，我没再逐句解释", cast=TANG_SORE_WRIST),
    cue("纱S09", "周素琴再来，坐在小桌旁", cast=TANG_SORE_WRIST + [ZHOU_EMPTY], room_sorted=True),
    cue("纱S09", "上一次她这么问，我只会哭", illustration="纱CG28"),
    cue("纱S09", "周素琴：我没说都是你不好", cast=TANG_SORE_WRIST + [ZHOU_LISTENING_EMPTY]),
    cue("纱S09", "说完我哭了。我想过很多次", illustration="纱CG29"),
    cue("纱S09", "她把照片看了一遍，合上", cast=TANG_SORE_WRIST),
    cue("纱S09", "十一月，转纱BG09", "bg09", YE, time="2029年11月 · 外地"),
    cue("纱S10", "手机亮起，秦嘉发来项目邮件", "bg09", YE, music="letter"),
    cue("A01", "第二天我和秦嘉聊工作", "bg09-work-area", ["ye-zhao-residency", "qin-jia-thinking"], time="2029年11月27日 · 工作讨论", tone="day"),
    cue("A01", "秦嘉：你先排一季度", cast=["ye-zhao-residency", "qin-jia"]),
    cue("A01", "我把文件带回房间", "bg09", YE, time="2029年11月27日 · 午后", tone="day"),
    cue("A01", "晚饭时，我拍了外地的最后一份盒饭", time="2029年11月27日 · 夜", tone="night"),
    cue("A01", "转店前小巷", "bg05-day", YE, time="2029年12月5日 · 归来", tone="day"),
    cue("A01", "门铃响时，见月抬起头", "bg03", BOTH, mood="warm"),
    cue("A02", "真的穿上外套", "bg04", MEASURING),
    cue("A02", "纱BG07。面条吃完", "bg07", HOME, tone="night"),
    cue("A03", "我们第三天就吵了一点", "bg07", HOME, time="2029年12月 · 深夜", tone="night", mood="thoughtful"),
    cue("A03", "我抱着电脑下楼", "bg03", HOME),
    cue("A03", "说出来的语气还是硬", illustration="纱CG25"),
    cue("A03", "洗衣也没那么容易", "bg13", ["ye-zhao-laundry", "tang-jianyue-laundry"], time="2029年12月 · 数日后白天", tone="day", mood="warm"),
    cue("A03", "好几件小事让我发现", "bg04", BOTH, mood="thoughtful"),
    cue("A03", "晚上整理柜子", "bg07", ["tang-jianyue-home"], time="2029年12月 · 夜", tone="night"),
    cue("A03", "我坐在地上，抱了一会儿", cast=HOME, illustration="纱CG23"),
    cue("A03", "我把旧衣服折好", illustration="纱CG24"),
    cue("A03", "晚饭最后吃了煮得太软的面", "bg07", HOME, tone="night", mood="thoughtful"),
    cue("A03", "月底，我们真的又坐下来", time="2029年12月末", mood="warm"),
    cue("A04", "我想提那间空房", "bg03", BOTH, tone="day"),
    cue("A04", "纱BG06布置差分", "bg06-temporary", BOTH, tone="day", mood="warm"),
    cue("A04", "临时共享工作室的第一天", illustration="纱CG13"),
    cue("A04", "关门后我们把时间表贴出来", "bg06-temporary", BOTH_EMPTY, time="2030年1月上旬 · 关门后", tone="night", mood="thoughtful"),
    cue("A04", "几天后，她用我的大桌裁好了", "bg06-temporary", BOTH, time="2030年1月上旬 · 数日后白天", tone="day", mood="warm"),
    cue("A05", "周素琴来的时候", cast=BOTH + [ZHOU], room_repaired=True),
    cue("A05", "周素琴：我回去还有事", cast=BOTH + [ZHOU_EMPTY]),
    cue("A05", "她不说留，也没有立刻走", "bg07", HOME + [ZHOU_EMPTY], time="2030年1月下旬 · 傍晚"),
    cue("A05", "那顿饭比我拍过", "bg07", HOME + [ZHOU_EMPTY], tone="night", illustration="纱CG30"),
    cue("A05", "周素琴：地滑，慢点", cast=HOME + [ZHOU_LISTENING_EMPTY]),
    cue("A05", "唐见月：住。我们一起", illustration="纱CG31"),
    cue("A05", "她走时仍不肯让我送", "bg07", WASHING_QUIET, tone="night"),
    cue("A05", "她低头。我把水关了", cast=HOME),
    cue("A05", "这话说出来，我自己先哭了", illustration="纱CG11"),
    cue("A05", "过了一会儿，我说菜做得有点多", "bg07", TEARFUL_HOME, tone="night"),
    cue("A05a", "睡觉时我把手机放远一点", "bg07", HOME, tone="night"),
    cue("A05a", "次日下午，纱BG06", "bg06", PHOTO_CAST, time="2030年1月末 · 次日午后", tone="day", mood="warm"),
    cue("A05a", "她拿着相机很小心", illustration="纱CG26"),
    cue("A05a", "叶照：要不要一起", "bg06", PHOTO_CAST),
    cue("A05a", "我跑到相机那里，回来时差点踩住凳脚", illustration="纱CG27"),
    cue("A05a", "我把这张洗出来", "bg03", PHOTO_CAST, time="2030年1月末 · 相片洗出后", tone="day", mood="warm"),
    cue("A06", "回去后我们为了钱吵了一小会儿", "bg07", HOME, mood="thoughtful"),
    cue("A06", "刘庆：你们两个这次都确定", cast=BOTH_EMPTY + ["liu-qing-confirming"]),
    cue("A06", "正式签约那天", "bg08", BOTH_EMPTY + ["liu-qing-confirming"], time="2030年2月 · 双方确认后", mood="warm"),
    cue("A06", "签约后刘庆先离开", cast=BOTH_EMPTY),
    cue("A06", "搬家一样搬工作台很乱", "bg08-furnished", BOTH_EMPTY, time="2030年3月 · 布置工作室"),
    cue("A07", "午后，纱BG08", "bg08-furnished", BOTH, tone="day"),
    cue("A07", "傍晚接纱BG10", "bg10", BOTH, time="2030年春 · 傍晚"),
    cue("A07", "回店后，她给玫瑰换水", "bg03", BOTH_EMPTY, tone="day"),
    cue("A07", "我在厨房问她明天吃什么", "bg07", HOME),
    cue("A07", "门铃响了一下。她在楼上叫我", "bg03", YE),
    cue("B02", "工作间。见月拿出外套", "bg04", BOTH),
    cue("B02", "三天内的两段夜晚", "bg07", HOME, tone="night"),
    cue("B02", "第三天早上，行李箱放到门边", "bg07", WASHING_QUIET, time="2029年12月 · 第三天早晨", tone="day"),
    cue("B02", "唐见月：走吧。到了告诉我", cast=HOME),
    cue("B02", "门铃响了一声，我拖箱子出去", "bg03", ["tang-jianyue-towel"], tone="day"),
    cue("B03", "房间比她想象的小", cast=["ye-zhao", "tang-jianyue-home"]),
    cue("B03", "第二日上午", time="2029年12月 · 探访第二天", tone="day"),
    cue("B03", "她真带了针线", illustration="纱CG14"),
    cue("B03", "两点没回来", "bg09", ["ye-zhao", "tang-jianyue-home"], time="2029年12月 · 探访第二天15:30", tone="day", mood="thoughtful"),
    cue("B03", "第三天外出散步", cast=["ye-zhao", "tang-jianyue-home"], time="2029年12月 · 探访第三天", tone="day", mood="thoughtful"),
    # The sleeping pair stays off stage; the room carries the quiet inner monologue.
    cue("B03", "晚上她睡着了", cast=[], time="2029年12月 · 探访第三夜", tone="night"),
    cue("B03", "第四天装箱", cast=YE, time="2029年12月 · 探访第四天午后", tone="day", mood="thoughtful"),
    cue("B04", "叶照回雁汀一天半", "bg03", BOTH_EMPTY, time="2030年1月 · 雁汀关店后", tone="night"),
    cue("B05", "当天晚上，不即时和好", "bg07", HOME, tone="night", mood="thoughtful"),
    cue("B05", "次日下午，纱BG04", "bg04", TANG, time="2030年2月 · 次日午后", tone="day"),
    cue("B05", "门铃响，她回来", cast=BOTH),
    cue("B06", "我哭了。她也哭", illustration="纱CG17"),
    cue("B06", "第二天早上她仍然做饭", "bg07", HOME, time="2030年2月中旬 · 决定后次日清晨", tone="day"),
    cue("B06", "她替我把外套改完", "bg04", ["ye-zhao-old-coat", "tang-jianyue-measuring"], time="2030年2月中旬 · 决定后次日午后", tone="day"),
    cue("B06", "晚上我们出去走了一小段", "bg05-day", ["ye-zhao-old-coat", "tang-jianyue-winter-empty"], time="2030年2月中旬 · 决定后次日夜间", tone="night"),
    cue("B06", "我笑了一下，眼泪又掉", cast=["ye-zhao-old-coat-tearful", "tang-jianyue-winter-empty"]),
    cue("B06", "回去时她手很冷", illustration="纱CG21"),
    cue("B07", "旧备用相机也拿了", cast=["ye-zhao-old-coat", "tang-jianyue-home"]),
    cue("B07", "楼下有客人敲门", "bg03", ["ye-zhao-old-coat", "tang-jianyue"]),
    cue("B07", "我拖起箱子", "bg05-day", ["ye-zhao-old-coat-departing"]),
    cue("B07", "上车以后，我发", "bg12", ["ye-zhao-old-coat-message"], time="2030年2月 · 返程车上", tone="day"),
    cue("B07", "回外地的第一周", "bg09", YE, time="2030年2月 · 外地"),
    cue("B07a", "同夜转见月视角", "bg07", ["tang-jianyue-home"]),
    cue("B07a", "我打过去。她接得很快", cast=PHONE),
    cue("B07a", "她先说了。我胸口猛地紧了一下", illustration="纱CG32"),
    cue("B07a", "我去捡充电线", "bg09", PHONE_CHARGING),
    cue("B07a", "挂断之后，我没有轻松起来", cast=YE, time="2030年春末 · 通话后"),
    cue("B07a", "我把自己那件穿旧的围裙折好", illustration="纱CG33"),
    cue("B07a", "那时候能让她多待一会儿", "bg07-lights-out", [], time="2030年春末 · 通话后关灯", tone="night-painted"),
    cue("B07a", "回叶照视角，沿工作出版", "bg09", YE, time="2031年 · 外地", tone="day"),
    cue("B08", "她说完笑。我笑着点头，把新外套穿上", cast=REUNION_WEARING),
    cue("B08", "取衣安排到两日后", "bg04", REUNION_WEARING, time="2033年冬 · 两日后"),
    cue("B08", "我们喝了一杯茶", "bg03-reunion", REUNION_WEARING),
    cue("B08", "走到巷口，我把手放进新外套口袋", "bg05-day", ["ye-zhao-reunion-pocket"]),
    cue("C01", "同夜，转纱BG04", "bg04", EMPTY_TANG),
    cue("C02", "次日，纱BG04", "bg04", EMPTY_TANG),
    cue("C03", "饭后纱BG07", "bg07", HOME, tone="night"),
    cue("C03", "次日上午，客人如约到来", "bg03", BOTH, time="2030年1月 · 次日午后", tone="day"),
    cue("C03", "到了外地才打开", "bg09", YE),
    cue("C04", "三月她发了一张屋顶修后的照片", time="2030年3月"),
    cue("C04", "五月，项目阶段展览确定", time="2030年5月"),
    cue("C04", "五月余下那些日子都很好", cast=PHONE),
    cue("C04", "六月上旬，见月先电话说明", cast=PHONE_TENSE, time="2030年6月上旬 · 通话"),
    cue("C04", "我挂断了电话。过了十分钟", cast=YE, time="2030年6月上旬 · 通话后"),
    cue("C05", "晚上回房，手机里有她", time="2030年6月12日 · 夜", tone="night"),
    cue("C05", "她打电话，我接了", cast=PHONE),
    cue("C05", "切见月视角。手机暗下", "bg04", EMPTY_TANG),
    cue("C05a", "唐玉芬又来了", cast=EMPTY_TANG + [AUNT_STERN], illustration="纱CG19"),
    cue("C05a", "我说得很突然", illustration="纱CG22", mood="thoughtful"),
    cue("C05a", "姨妈气得走了", "bg03-ironing", EMPTY_TANG),
    cue("C05a", "我把消息打出来", cast=["tang-jianyue-work-message"]),
    cue("C05a", "熨斗放着不能一直开", cast=EMPTY_TANG),
    cue("C05a", "我给叶照发了一句", cast=["tang-jianyue-work-message"]),
    cue("C05a", "我坐下来继续缝衣", illustration="纱CG20"),
    cue("C06", "过几天，我们打了一次长电话", cast=PHONE_TENSE, time="2030年8月 · 通话"),
    cue("C06", "我没有补上解释。以为今晚停住", cast=YE, time="2030年8月 · 通话后"),
    cue("C06", "十一月有一晚", time="2030年11月 · 夜"),
    cue("C07", "有一晚下大雨", time="2031年春 · 雨夜", tone="rain-night"),
    cue("C07", "我在照片里看到一对普通人", time="2031年春 · 看照片时", tone="day"),
    cue("C07", "那晚我很想她", time="2031年春 · 夜至翌晨", tone="night"),
    cue("C07", "后来我不再每天想起她", time="2031年至2032年 · 外地", tone="day"),
    cue("C07", "2032年秋，衣坊", "bg07", ["tang-jianyue-home"], time="2032年秋 · 雁汀"),
    cue("C08", "我坐在工作台旁，终于哭了", illustration="纱CG15"),
    cue("C08", "客人取衣通过旁白处理", "bg04", LATE_TANG),
    cue("C08", "同周，纱BG09", "bg11", YE, time="2033年冬 · 同周外地"),
    cue("C08", "我把手伸进右内侧", cast=["ye-zhao-returned-coat-crying"]),
    cue("C08", "原来她那时候已经做了", illustration="纱CG16"),
    cue("C08", "我拿起手机，想告诉她看见口袋了", "bg11", ["ye-zhao-returned-coat-message"]),
    cue("C08", "我把外套叠好，放在椅子上", cast=YE),
    cue("C08", "回纱BG04，见月视角", "bg04", LATE_TANG, time="2033年冬 · 雁汀收店后", tone="night"),
]
