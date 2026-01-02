using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIOSK.FSM
{
    // 단 4개의 트리거만 사용
    public enum StateMachineTrigger
    {
        Next,           // 다음 화면/단계로 진행
        Previous,       // 이전 화면으로 이동 ( History )
        Exit,           // 메인 화면(복귀)
        Error           // 에러 발생 -> 에러 화면
    }
}
