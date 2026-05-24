#!/bin/bash
# Unity 소울라이크 프로젝트 로컬 적용 스크립트
# 사용법: bash setup.sh <Unity프로젝트경로>
# 예시:   bash setup.sh ~/UnityProjects/MySoulsGame

set -e

UNITY_PROJECT="${1:-}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

if [ -z "$UNITY_PROJECT" ]; then
    echo "사용법: bash setup.sh <Unity프로젝트경로>"
    echo "예시:   bash setup.sh ~/UnityProjects/MySoulsGame"
    exit 1
fi

if [ ! -d "$UNITY_PROJECT/Assets" ]; then
    echo "오류: Unity 프로젝트를 찾을 수 없습니다 — $UNITY_PROJECT/Assets 가 없습니다."
    echo "Unity Hub에서 새 3D 프로젝트를 먼저 생성하세요."
    exit 1
fi

echo "=== Unity 소울라이크 스크립트 복사 중 ==="
cp -r "$SCRIPT_DIR/Assets/Scripts" "$UNITY_PROJECT/Assets/"
cp -r "$SCRIPT_DIR/Assets/Editor"  "$UNITY_PROJECT/Assets/"

echo "✓ 스크립트 복사 완료"
echo ""
echo "=== 다음 단계 ==="
echo "1. Unity Editor를 열고 프로젝트를 로드하세요."
echo "2. 컴파일이 완료되면 상단 메뉴에서:"
echo "   [Soulslike] > [Build Scene (Auto Setup)] 클릭"
echo "3. 씬이 자동으로 구성됩니다!"
echo ""
echo "완료!"
