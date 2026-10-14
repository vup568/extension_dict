import type { ConjugationExplanation } from "../../src/api/analysisContract.ts";

// Test-only snapshot from frozen corpus .3; runtime code must not import this file.
export const sourceCasesSha256 = "ca3e3ff71b4d97a96e33c70695d5cfb067244bb6cf63a7d3340cd901dd7883c4";
export const approvedConjugationExamples: Record<string, { canonicalId: string; baseForm: string; glossVi: string; readings: string[]; conjugations: ConjugationExplanation[] }> = {
  "食べられる": {
    "canonicalId": "fixture:word:001",
    "baseForm": "食べる",
    "glossVi": "Ăn",
    "readings": [
      "たべる"
    ],
    "conjugations": [
      {
        "surface": "食べられる",
        "base_form": "食べる",
        "explanation": "Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; không quá khứ, thông thường. Dạng gốc: 食べる.",
        "language_tag": "vi",
        "span": {
          "start": 0,
          "end": 5,
          "unit": "utf16"
        },
        "rule_id": "conjugation:verb-passive",
        "rule_version": 1,
        "dataset_version": "candidate-20261013.1",
        "segments": [
          {
            "surface": "食べ",
            "explanation": "Phần động từ của 食べる ở dạng quan sát này.",
            "span": {
              "start": 0,
              "end": 2,
              "unit": "utf16"
            }
          },
          {
            "surface": "られ",
            "explanation": "Phần bị động trong cách hiểu này.",
            "span": {
              "start": 2,
              "end": 4,
              "unit": "utf16"
            }
          },
          {
            "surface": "る",
            "explanation": "Đuôi không quá khứ của dạng đang được phân tích.",
            "span": {
              "start": 4,
              "end": 5,
              "unit": "utf16"
            }
          }
        ]
      },
      {
        "surface": "食べられる",
        "base_form": "食べる",
        "explanation": "Thể khả năng: có thể thực hiện hành động; không quá khứ, thông thường. Dạng gốc: 食べる.",
        "language_tag": "vi",
        "span": {
          "start": 0,
          "end": 5,
          "unit": "utf16"
        },
        "rule_id": "conjugation:verb-potential",
        "rule_version": 1,
        "dataset_version": "candidate-20261013.1",
        "segments": [
          {
            "surface": "食べ",
            "explanation": "Phần động từ của 食べる ở dạng quan sát này.",
            "span": {
              "start": 0,
              "end": 2,
              "unit": "utf16"
            }
          },
          {
            "surface": "られ",
            "explanation": "Phần khả năng trong cách hiểu này.",
            "span": {
              "start": 2,
              "end": 4,
              "unit": "utf16"
            }
          },
          {
            "surface": "る",
            "explanation": "Đuôi không quá khứ của dạng đang được phân tích.",
            "span": {
              "start": 4,
              "end": 5,
              "unit": "utf16"
            }
          }
        ]
      },
      {
        "surface": "食べられる",
        "base_form": "食べる",
        "explanation": "Kính ngữ tôn kính: diễn đạt hành động của người được kính trọng; không mặc định là bị động. không mặc định là bất lợi; không quá khứ, thông thường. Dạng gốc: 食べる.",
        "language_tag": "vi",
        "span": {
          "start": 0,
          "end": 5,
          "unit": "utf16"
        },
        "rule_id": "conjugation:verb-honorific",
        "rule_version": 1,
        "dataset_version": "candidate-20261013.1",
        "segments": [
          {
            "surface": "食べ",
            "explanation": "Phần động từ của 食べる ở dạng quan sát này.",
            "span": {
              "start": 0,
              "end": 2,
              "unit": "utf16"
            }
          },
          {
            "surface": "られ",
            "explanation": "Phần tôn kính trong cách hiểu này; thể hiện sự kính trọng đối với người thực hiện hành động.",
            "span": {
              "start": 2,
              "end": 4,
              "unit": "utf16"
            }
          },
          {
            "surface": "る",
            "explanation": "Đuôi không quá khứ của dạng đang được phân tích.",
            "span": {
              "start": 4,
              "end": 5,
              "unit": "utf16"
            }
          }
        ]
      }
    ]
  },
  "食べさせられる": {
    "canonicalId": "fixture:word:001",
    "baseForm": "食べる",
    "glossVi": "Ăn",
    "readings": [
      "たべる"
    ],
    "conjugations": [
      {
        "surface": "食べさせられる",
        "base_form": "食べる",
        "explanation": "Thể sai khiến-bị động: bị yêu cầu hoặc bắt thực hiện hành động; không quá khứ, thông thường. Dạng gốc: 食べる.",
        "language_tag": "vi",
        "span": {
          "start": 0,
          "end": 7,
          "unit": "utf16"
        },
        "rule_id": "conjugation:verb-causative-passive",
        "rule_version": 1,
        "dataset_version": "candidate-20261013.1",
        "segments": [
          {
            "surface": "食べ",
            "explanation": "Phần động từ của 食べる ở dạng quan sát này.",
            "span": {
              "start": 0,
              "end": 2,
              "unit": "utf16"
            }
          },
          {
            "surface": "させ",
            "explanation": "Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu.",
            "span": {
              "start": 2,
              "end": 4,
              "unit": "utf16"
            }
          },
          {
            "surface": "られ",
            "explanation": "Phần bị động trong cách hiểu này.",
            "span": {
              "start": 4,
              "end": 6,
              "unit": "utf16"
            }
          },
          {
            "surface": "る",
            "explanation": "Đuôi không quá khứ của dạng đang được phân tích.",
            "span": {
              "start": 6,
              "end": 7,
              "unit": "utf16"
            }
          }
        ]
      },
      {
        "surface": "食べさせられる",
        "base_form": "食べる",
        "explanation": "Sai khiến kết hợp khả năng: có thể cho phép/khiến thực hiện hành động; không quá khứ, thông thường. Dạng gốc: 食べる.",
        "language_tag": "vi",
        "span": {
          "start": 0,
          "end": 7,
          "unit": "utf16"
        },
        "rule_id": "conjugation:verb-causative-potential",
        "rule_version": 1,
        "dataset_version": "candidate-20261013.1",
        "segments": [
          {
            "surface": "食べ",
            "explanation": "Phần động từ của 食べる ở dạng quan sát này.",
            "span": {
              "start": 0,
              "end": 2,
              "unit": "utf16"
            }
          },
          {
            "surface": "させ",
            "explanation": "Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu.",
            "span": {
              "start": 2,
              "end": 4,
              "unit": "utf16"
            }
          },
          {
            "surface": "られ",
            "explanation": "Phần khả năng của dạng sai khiến trong cách hiểu này.",
            "span": {
              "start": 4,
              "end": 6,
              "unit": "utf16"
            }
          },
          {
            "surface": "る",
            "explanation": "Đuôi không quá khứ của dạng đang được phân tích.",
            "span": {
              "start": 6,
              "end": 7,
              "unit": "utf16"
            }
          }
        ]
      }
    ]
  },
  "静かです": {
    "canonicalId": "fixture:word:010",
    "baseForm": "静か",
    "glossVi": "Yên tĩnh",
    "readings": [
      "しずか"
    ],
    "conjugations": [
      {
        "surface": "静かです",
        "base_form": "静か",
        "explanation": "Tính từ な: khẳng định lịch sự, không quá khứ. Dạng gốc: 静か.",
        "language_tag": "vi",
        "span": {
          "start": 0,
          "end": 4,
          "unit": "utf16"
        },
        "rule_id": "conjugation:na-copula-polite-nonpast",
        "rule_version": 1,
        "dataset_version": "candidate-20261013.1",
        "segments": [
          {
            "surface": "静か",
            "explanation": "Phần tính từ な của 静か ở dạng quan sát này.",
            "span": {
              "start": 0,
              "end": 2,
              "unit": "utf16"
            }
          },
          {
            "surface": "です",
            "explanation": "Copula khẳng định lịch sự, không quá khứ.",
            "span": {
              "start": 2,
              "end": 4,
              "unit": "utf16"
            }
          }
        ]
      }
    ]
  },
  "学生です": {
    "canonicalId": "fixture:word:013",
    "baseForm": "学生",
    "glossVi": "Học sinh/sinh viên",
    "readings": [
      "がくせい"
    ],
    "conjugations": [
      {
        "surface": "学生です",
        "base_form": "学生",
        "explanation": "Danh từ đi với copula; bản thân danh từ không tự chia: khẳng định lịch sự, không quá khứ. Dạng gốc: 学生.",
        "language_tag": "vi",
        "span": {
          "start": 0,
          "end": 4,
          "unit": "utf16"
        },
        "rule_id": "conjugation:noun-copula-polite-nonpast",
        "rule_version": 1,
        "dataset_version": "candidate-20261013.1",
        "segments": [
          {
            "surface": "学生",
            "explanation": "Phần danh từ của 学生 ở dạng quan sát này.",
            "span": {
              "start": 0,
              "end": 2,
              "unit": "utf16"
            }
          },
          {
            "surface": "です",
            "explanation": "Copula khẳng định lịch sự, không quá khứ.",
            "span": {
              "start": 2,
              "end": 4,
              "unit": "utf16"
            }
          }
        ]
      }
    ]
  },
  "𠮷です": {
    "canonicalId": "fixture:word:015",
    "baseForm": "𠮷",
    "glossVi": "Tên riêng tổng hợp dùng để test UTF-16",
    "readings": [
      "よし"
    ],
    "conjugations": [
      {
        "surface": "𠮷です",
        "base_form": "𠮷",
        "explanation": "Danh từ đi với copula; bản thân danh từ không tự chia: khẳng định lịch sự, không quá khứ. Dạng gốc: 𠮷.",
        "language_tag": "vi",
        "span": {
          "start": 0,
          "end": 4,
          "unit": "utf16"
        },
        "rule_id": "conjugation:noun-copula-polite-nonpast",
        "rule_version": 1,
        "dataset_version": "candidate-20261013.1",
        "segments": [
          {
            "surface": "𠮷",
            "explanation": "Phần danh từ của 𠮷 ở dạng quan sát này.",
            "span": {
              "start": 0,
              "end": 2,
              "unit": "utf16"
            }
          },
          {
            "surface": "です",
            "explanation": "Copula khẳng định lịch sự, không quá khứ.",
            "span": {
              "start": 2,
              "end": 4,
              "unit": "utf16"
            }
          }
        ]
      }
    ]
  }
};

