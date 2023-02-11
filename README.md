코드 스타일
========
* 코드 스타일 공유를 위해 개인 연구용 프로젝트의 일부 코드를 공유하는 저장소 입니다.
* 스타일 참조를 위해 코드의 일부만 공유 드립니다. 완전하지 않은 부분 양해 부탁드립니다.

*Sections*
--
* Unity Scene을 1개이상 관리하는 역활을 하는 Class들 입니다.
* Section Class를 상속 받은 Scene을 관리하는 Class로 부터 각 Scene이 시작
* Service에서 전달된 Message를 수신하여 필요한 UI혹은 View에 전달하여 화면에 표현할 수 있도록 합니다.

*Networks*
--
* 패킷등 통신대한 구조를 보여주는 Class들 입니다.
* NetworkService를 상속받는 Service들을 통하여 통신을 진행하고 INetworkProvidor를 통하여 관리됩니다.
  
*Services*
--
* Network를 통해 전달되는 패킷들을 처리하는 단위를 Service라 명명.
* 역할별로 (ex.Battle, Room, User등) 구분되어 정의
* 역할별 패킷 송수신 담당합니다.
* 수신된 패킷을 기반으로 Processor에서 처리
* 송수신 데이터는 Entity에 저장
* 화면에 표현은 MessageQueue를 통하여 Section에 전달하여 View혹은 UI에서 처리합니다.
