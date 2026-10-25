"""Compile the authoritative Rose scripts to MonoGalGame's AOT-friendly story graph.

Only the four playable scripts are inputs. Direction, route labels and production
notes never become dialogue. The checked-in JSON lets players run without Python.
"""
from __future__ import annotations

import argparse
import copy
import hashlib
import json
import re
from pathlib import Path
from rose_staging import CG, SCRIPT_CG, SUPPLEMENTAL_CG, CUES, SECTIONS, REFLECTIVE, NIGHT_BACKGROUNDS

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "docs/玫瑰与旧婚纱/设计稿/剧情设计"
GAME = ROOT / "examples/MonoGalGame.Rose/Game"
SCRIPTS = ["01_共通剧情.txt", "03_分支A_两人的尺寸.txt",
           "04_分支B_合身的告别.txt", "05_分支C_没有日期的以后.txt"]
LOCATIONS = {
    "bg01": "婚宴会馆 · 储物间", "bg02": "婚宴会馆 · 准备室",
    "bg03": "见月衣坊 · 前厅", "bg04": "见月衣坊 · 工作间",
    "bg05": "衣坊门前 · 小巷", "bg06": "叶照的摄影工作室",
    "bg07": "见月衣坊 · 楼上", "bg08": "共同工作室",
    "bg09": "外地 · 驻留室", "bg10": "旧城 · 婚纱橱窗街",
    "bg05-day": "衣坊门前 · 小巷", "bg08-furnished": "共同工作室",
    "bg06-temporary": "叶照的摄影工作室 · 临时借用",
    "bg07-rain-night": "见月衣坊 · 楼上", "bg07-rain-dawn": "见月衣坊 · 楼上",
    "bg09-work-area": "驻留项目 · 公共工作区",
    "bg07-january-leak": "见月衣坊 · 楼上待修",
    "bg02-hallway": "婚宴会馆 · 走廊与宴会入口",
    "bg11": "外地 · 新住处",
    "bg03-reunion": "见月衣坊 · 前厅",
    "bg12": "返程 · 车厢",
    "bg03-ironing": "见月衣坊 · 前厅",
    "bg13": "见月衣坊 · 楼上阳台",
    "bg07-lights-out": "见月衣坊 · 楼上",
}
ROUTES = {"A": "两人的尺寸", "B": "合身的告别", "C": "没有日期的以后"}
MUSIC = {"纱S01": "winter", "纱S02": "atelier", "纱S03": "atelier",
         "纱S04": "tender", "纱S05": "tender", "纱S06": "",
         "纱S07": "distance", "纱S08": "distance", "纱S09": "letter",
         "纱S10": "letter"}


class Compiler:
    def __init__(self):
        self.nodes = {}
        self.tail = None
        self.section = ""
        self.chapter = ""
        self.state = dict(background="bg01", portrait="", secondaryPortrait="", tertiaryPortrait="",
                          music="winter", chapter="", location=LOCATIONS["bg01"],
                          time="2029年1月18日 · 15:26", viewpoint="叶照", tone="day")
        self.memory = None
        self.counts = {}
        self.source_lines = []
        self.active_cg = False
        self.cast = []
        self.mood = "warm"
        self.room_sorted = False
        self.room_repaired = False
        self.cue_hits = [0] * len(CUES)
        self.cg_hits = set()
        self.audit = []

    def append(self, node, node_id=None):
        if node_id is None:
            self.counts[self.section] = self.counts.get(self.section, 0) + 1
            node_id = f"{self.section}-{self.counts[self.section]:03d}"
        if node_id in self.nodes:
            raise ValueError(f"Duplicate node: {node_id}")
        if self.tail:
            self.nodes[self.tail]["next"] = node_id
        self.nodes[node_id] = node
        self.tail = node_id if node.get("kind", "Dialogue") not in ("Choice", "End", "Branch", "Jump") else None
        return node_id

    def background(self, bg):
        self.state.update(background=bg, location=LOCATIONS[bg])
        self.active_cg = False

    def stage(self, line):
        for index, cue in enumerate(CUES):
            if cue["section"] != self.section or cue["anchor"] not in line:
                continue
            self.cue_hits[index] += 1
            if cue["background"] is not None:
                self.background(cue["background"])
            if cue["cast"] is not None:
                self.cast = list(cue["cast"])
            if cue["mood"] is not None:
                self.mood = cue["mood"]
            if cue["room_sorted"] is not None:
                self.room_sorted = cue["room_sorted"]
            if cue["room_repaired"] is not None:
                self.room_repaired = cue["room_repaired"]
            if cue["illustration"] is not None:
                self.show_cg(cue["illustration"])
            for field in ("time", "tone", "music"):
                if cue[field] is not None:
                    self.state[field] = cue[field]

    def show_cg(self, text):
        match = re.search(r"纱CG\d+[ABC]?", text)
        if match:
            if match[0] not in CG:
                raise ValueError(f"Unmapped CG: {match[0]}")
            self.state["background"] = CG[match[0]]
            self.active_cg = True
            self.cg_hits.add(match[0])

    def directions(self, text, scene=False):
        if "音乐远去" in text or "不切浪漫配乐" in text or "阅读过程中保持安静" in text:
            self.state["music"] = ""
        elif "片尾音乐" in text:
            self.state["music"] = "letter"
        if "灯灭" in text:
            self.state["tone"] = "dim"
        # This inline CG is an actual cue; mentions promising a later CG are not.
        if "纱CG09显示内袋" in text:
            self.show_cg(text)

    def dialogue(self, speaker, text, line_number, filename):
        if speaker.startswith("旁白"):
            self.state["viewpoint"] = "唐见月" if "见月" in speaker else "叶照"
        scene = copy.deepcopy(self.state)
        if self.active_cg:
            scene.update(portrait="", secondaryPortrait="", tertiaryPortrait="", tone="day")
        else:
            if scene["background"] == "bg07":
                if self.room_repaired:
                    scene["background"] = "bg07-repaired"
                elif self.room_sorted:
                    scene["background"] = "bg07-sorted"
            spring = re.search(r"年(?:3|4|5)月|年春", scene["time"])
            summer = re.search(r"年(?:6|7|8)月|年夏", scene["time"])
            if scene["background"] == "bg03":
                if spring:
                    scene["background"] = "bg03-spring"
                elif summer:
                    scene["background"] = "bg03-summer"
            if scene["background"] == "bg07-repaired" and spring:
                scene["background"] = "bg07-repaired-spring"
            autumn = re.search(r"年(?:9|10|11)月|年秋", scene["time"])
            if autumn and scene["tone"] == "day" and scene["background"] in ("bg07", "bg07-sorted"):
                scene["background"] = {"bg07": "bg07-autumn", "bg07-sorted": "bg07-sorted-autumn"}[scene["background"]]
            winter = re.search(r"年(?:12|1|2)月|年冬(?:[ ·]|$)", scene["time"])
            if winter and scene["background"] in ("bg04", "bg05-day", "bg06", "bg07-sorted", "bg08"):
                scene["background"] = {"bg04": "bg04-winter", "bg05-day": "bg05-winter", "bg06": "bg06-winter", "bg07-sorted": "bg07-sorted-winter", "bg08": "bg08-winter"}[scene["background"]]
            if scene["background"] == "bg09":
                for pattern, variant in [(r"年(?:12|1|2)月|年冬(?:[ ·]|$)", "winter"),
                                         (r"年(?:3|4|5)月|年春", "spring"),
                                         (r"年(?:6|7|8)月|年夏", "summer")]:
                    if re.search(pattern, scene["time"]):
                        scene["background"] = "bg09-" + variant
                        break
            dressed = ["ye-zhao-residency" if id == "ye-zhao" and scene["background"] in ("bg09", "bg09-winter", "bg09-spring", "bg09-summer", "bg11") else id for id in self.cast]
            posed = [id + "-thoughtful" if self.mood == "thoughtful" and id in
                     ("ye-zhao", "tang-jianyue", "tang-jianyue-empty", "ye-zhao-home", "tang-jianyue-home", "ye-zhao-residency") else id for id in dressed]
            tang = next((id for id in posed if id.startswith("tang-jianyue")), "")
            ye = next((id for id in posed if id.startswith("ye-zhao")), "")
            supporting = [id for id in posed if id not in (tang, ye)]
            if len(posed) > 3:
                raise ValueError(f"Too many actors: {self.section} / {text}")
            primary = tang or ye or (supporting.pop() if supporting else "")
            secondary = ye if tang else ""
            if not secondary and supporting:
                secondary = supporting.pop(0)
            scene.update(portrait=primary, secondaryPortrait=secondary,
                         tertiaryPortrait=supporting[0] if supporting else "")
            night_worklight = scene["background"] in ("bg04", "bg04-winter") and scene["tone"] == "cool" and "夜" in scene["time"]
            if scene["background"] in NIGHT_BACKGROUNDS and (scene["tone"] in ("night", "rain-night", "dim") or night_worklight):
                scene["background"] = NIGHT_BACKGROUNDS[scene["background"]]
                if scene["tone"] != "dim":
                    scene["tone"] += "-painted"
        # A letter/phone speaker retains the channel; inner voice is visibly named.
        node_id = self.append(dict(speaker=speaker, text=text, scene=scene))
        self.source_lines.append(dict(file=filename, line=line_number, speaker=speaker, text=text))
        self.audit.append(dict(node=node_id, file=filename, line=line_number,
                               background=scene["background"], cast=list(self.cast),
                               time=scene["time"], viewpoint=scene["viewpoint"], mood=self.mood,
                               portrait=scene["portrait"], secondaryPortrait=scene["secondaryPortrait"],
                               tertiaryPortrait=scene["tertiaryPortrait"], tone=scene["tone"], roomSorted=self.room_sorted, roomRepaired=self.room_repaired))

    def begin_memory(self, question, side):
        if side == "A":
            branch = self.append(dict(kind="Branch", condition=dict(variable=f"q{question}", value=1)))
            self.memory = dict(branch=branch, baseline=copy.deepcopy(self.state), cast=list(self.cast), cg=self.active_cg, mood=self.mood, room_sorted=self.room_sorted, room_repaired=self.room_repaired)
            then_id = self.append(dict(kind="Set"))
            self.nodes[branch]["then"] = then_id
        else:
            if not self.memory:
                raise ValueError("Unpaired conditional response")
            self.memory["a_tail"] = self.tail
            self.tail = None
            self.state = copy.deepcopy(self.memory["baseline"])
            self.cast = list(self.memory["cast"])
            self.active_cg = self.memory["cg"]
            self.mood = self.memory["mood"]
            self.room_sorted = self.memory["room_sorted"]
            self.room_repaired = self.memory["room_repaired"]
            else_id = self.append(dict(kind="Set"))
            self.nodes[self.memory["branch"]]["else"] = else_id

    def join_memory(self):
        if not self.memory:
            raise ValueError("Unexpected memory join")
        join = self.append(dict(kind="Set"))
        self.nodes[self.memory["a_tail"]]["next"] = join
        self.memory = None

    def compile_file(self, filename):
        lines = (SOURCE / filename).read_text(encoding="utf-8-sig").splitlines()
        route = re.search(r"分支([ABC])_", filename)
        if route:
            self.chapter = "终章 · " + ROUTES[route[1]]
            # Every route begins after the common October sorting, independently of the previously compiled route.
            self.room_sorted = True
            self.room_repaired = False
        for number, raw in enumerate(lines, 1):
            line = raw.strip()
            if not line or line.startswith(("《", "阅读入口：", "入口：", "→")):
                continue
            if re.match(r"^第.+章", line):
                self.chapter = re.sub(r"\s+", " · ", line, count=1)
                continue
            section = re.match(r"^(纱S\d+|[ABC]\d+a?)｜(.+)$", line)
            if section:
                if self.memory:
                    raise ValueError("Unclosed conditional block")
                self.section = section[1]
                bg, time, cast, tone = SECTIONS[self.section]
                self.background(bg)
                self.cast = list(cast)
                self.mood = "thoughtful" if self.section in REFLECTIVE else "warm"
                self.state.update(time=time, tone=tone, viewpoint="叶照")
                self.state["chapter"] = f"{self.chapter} / {section[2]}"
                self.state["music"] = MUSIC.get(self.section, {"A": "tender", "B": "letter", "C": "distance"}.get(self.section[0], "atelier"))
                self.active_cg = False
                self.append(dict(kind="Set"), self.section)
                continue
            self.stage(line)
            choice = re.match(r"【选项Q(\d)", line)
            if choice:
                q = choice[1]
                text = {"1": "她还想说什么。我该怎样靠近她？", "2": "等过这一下午，我想对她说……", "3": "这一次，我想怎样回答她？"}[q]
                scene = copy.deepcopy(self.nodes[self.tail].get("scene", self.state))
                self.append(dict(kind="Choice", speaker="叶照", text=text, scene=scene, choices=[]), f"q{q}")
                continue
            option = re.match(r"^([123])([ABC])「(.+)」$", line)
            if option:
                q, side, text = option.groups()
                target = f"{side}01" if q == "3" else f"q{q}-{side.lower()}"
                self.nodes[f"q{q}"]["choices"].append(dict(text=text, next=target, effects=[dict(variable=f"q{q}", value=ord(side)-64)]))
                continue
            reaction = re.match(r"【Q(\d)([AB])反应", line)
            if reaction:
                self.tail = None
                self.append(dict(kind="Set"), f"q{reaction[1]}-{reaction[2].lower()}")
                continue
            memory = re.match(r"【条件演出Q(\d)([AB])】", line)
            if memory:
                self.begin_memory(*memory.groups())
                continue
            if line in ("【合流】", "【合流对白】"):
                self.join_memory()
                continue
            if line.startswith("【跳转】"):
                target = re.search(r"纱S\d+", line)[0]
                self.append(dict(kind="Jump", next=target))
                continue
            if line.startswith("【结局】"):
                self.append(dict(kind="End", text=line[4:].split("／")[0], scene=copy.deepcopy(self.state)), f"ending-{route[1].lower()}")
                continue
            narration = re.match(r"【旁白·(.+?)】(.*)$", line)
            if narration:
                self.dialogue("旁白 · " + ("唐见月" if narration[1] == "见月" else narration[1]), narration[2], number, filename)
                continue
            stage = re.match(r"【(场景|演出|CG|本章收束|制作备注)】(.*)$", line)
            if stage:
                kind, content = stage.groups()
                if kind in ("场景", "演出"):
                    self.directions(content, kind == "场景")
                elif kind == "CG":
                    self.show_cg(content)
                continue
            spoken = re.match(r"^([^：]{1,24})：(.*)$", line)
            if spoken:
                self.dialogue(*spoken.groups(), number, filename)
                continue
            raise ValueError(f"Unrecognized source at {filename}:{number}: {line}")

    def build(self):
        for filename in SCRIPTS:
            self.tail = None
            self.compile_file(filename)
        for cue, hits in zip(CUES, self.cue_hits):
            if hits != 1:
                raise ValueError(f"Staging cue must match exactly once ({hits}): {cue['section']} / {cue['anchor']}")
        if self.cg_hits != set(CG):
            raise ValueError(f"Missing CG cues: {set(CG) - self.cg_hits}")
        displayed = {node.get("scene", {}).get("background") for node in self.nodes.values()}
        if not set(CG.values()) <= displayed:
            raise ValueError(f"A CG has no visible dialogue node: {set(CG.values()) - displayed}")
        for node_id, node in self.nodes.items():
            kind = node.get("kind", "Dialogue")
            targets = ([c["next"] for c in node["choices"]] if kind == "Choice" else
                       [node["then"], node["else"]] if kind == "Branch" else
                       [] if kind == "End" else [node.get("next")])
            for target in targets:
                if target not in self.nodes:
                    raise ValueError(f"{node_id} points to missing {target}")
        return dict(id="rose-and-old-wedding-dress-v1", title="玫瑰与旧婚纱", start="纱S01", nodes=self.nodes)


def source_sha256(path):
    # Git checkouts use CRLF on Windows and LF on Linux/macOS. Hash the same text.
    return hashlib.sha256(path.read_text(encoding="utf-8").encode("utf-8")).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="Check that committed content matches the source scripts")
    args = parser.parse_args()
    compiler = Compiler()
    story = compiler.build()
    manifest = dict(sourceHashFormat="utf8-lf", sources={name: source_sha256(SOURCE / name) for name in SCRIPTS},
                    dialogueLines=len(compiler.source_lines), nodes=len(story["nodes"]),
                    choices=["q1", "q2", "q3"], endings=["ending-a", "ending-b", "ending-c"],
                    stagingSha256=source_sha256(ROOT / "scripts/rose_staging.py"),
                    stagingCues=len(CUES), storyIllustrations=len(CG),
                    scriptStoryIllustrations=len(SCRIPT_CG), supplementalStoryIllustrations=len(SUPPLEMENTAL_CG),
                    compilerSha256=source_sha256(Path(__file__)))
    outputs = {GAME / "Stories/main.json": story, GAME / "Stories/source-manifest.json": manifest,
               GAME / "Stories/staging-manifest.json": compiler.audit}
    for path, value in outputs.items():
        text = json.dumps(value, ensure_ascii=False, indent=2) + "\n"
        if args.check:
            if not path.exists() or path.read_text(encoding="utf-8") != text:
                raise SystemExit(f"Outdated: {path.relative_to(ROOT)}")
        else:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(text, encoding="utf-8", newline="\n")
    print(f"Rose scripts: {len(story['nodes'])} nodes, {len(compiler.source_lines)} dialogue lines, 3 choices, 3 endings.")


if __name__ == "__main__":
    main()
