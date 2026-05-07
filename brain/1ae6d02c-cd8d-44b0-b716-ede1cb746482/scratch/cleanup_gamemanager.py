import os
import re

file_path = r'd:\GitHub\Dice-Rush\Assets\Scripts\GameManager.cs'

with open(file_path, 'r', encoding='utf-8') as f:
    lines = f.readlines()

new_lines = []
skip_methods = [
    'FetchDisconnectedPlayerScores',
    'SyncMatchDataToPlayFab',
    'ReSyncScoreToPhoton',
    'OnOfflineRewardClaimed',
    'CacheNetworkPlayerNames',
    'DelayedReconnectSync'
]

# 1. Update Namespaces
for i in range(len(lines)):
    if 'using Photon.Pun;' in lines[i]:
        lines[i] = ''
    if 'using System.Reflection;' in lines[i]:
        lines[i] = ''

# 2. Add Socket fields and remove Photon fields
fields_to_remove = [
    'cachedAbsoluteEndTime',
    'cachedPhotonTimeAtCapture',
    'cachedRealTimeAtCapture',
    'wasInRoomLastFrame',
    'currentActorNumberAtStart',
    'lastKnownPlayFabIds',
    'playerRecoveryData',
    'reconnectSyncCoroutine'
]

# We'll do a simple text replacement for the fields block
content = "".join(lines)

# Remove the fields
for field in fields_to_remove:
    content = re.sub(rf'private.*{field}.*;', '', content)

# Add new fields if not present
if 'private Dictionary<string, int> networkPlayerScores' not in content:
    content = content.replace('private HashSet<int> participantsAtStart = new HashSet<int>();', 
                             'private HashSet<int> participantsAtStart = new HashSet<int>();\n    private Dictionary<string, int> networkPlayerScores = new Dictionary<string, int>();\n    private Dictionary<string, double> networkPlayerTimes = new Dictionary<string, double>();')

# 3. Remove Photon/PlayFab logic from specific methods
# Start()
content = re.sub(r'// SILENT RECONNECT DETECTOR.*?wasInRoomLastFrame = isInRoomNow;', '', content, flags=re.DOTALL)
content = content.replace('MatchmakingModel.OnNumberLockedEvent += HandleServerNumberLocked;', '') # Remove duplicate if any
content = content.replace('quitPanel.SetActive(false);', 'quitPanel.SetActive(false);\n        MatchmakingModel.OnNumberLockedEvent += HandleServerNumberLocked;')

# StartGameTimer()
content = re.sub(r'CacheNetworkPlayerNames\(\);.*?currentActorNumberAtStart = -1; // Solo play\s+\}', 
                 'participantsAtStart.Clear();\n        networkPlayerScores.Clear();\n        networkPlayerTimes.Clear();', content, flags=re.DOTALL)

# OnDestroy()
content = re.sub(r'if \(PlayFabManager\.Instance != null\).*?\}', 'MatchmakingModel.OnNumberLockedEvent -= HandleServerNumberLocked;', content, flags=re.DOTALL)

# GetRankings()
# We already replaced this but let's make sure it's clean
# (Omitting for now to avoid breaking the previous good edit)

# 4. Remove all PhotonNetwork and LobbyNetworkManager.Instance calls
content = re.sub(r'if \(Photon\.Pun\.PhotonNetwork\.InRoom\).*?\}', '', content, flags=re.DOTALL)
content = re.sub(r'if \(PhotonNetwork\.InRoom\).*?\}', '', content, flags=re.DOTALL)
content = re.sub(r'if \(LobbyNetworkManager\.Instance != null\).*?\}', '', content, flags=re.DOTALL)
content = re.sub(r'Photon\.Pun\.PhotonNetwork\..*?;', '', content)
content = re.sub(r'PhotonNetwork\..*?;', '', content)

# 5. Remove the obsolete methods entirely
for method in skip_methods:
    content = re.sub(rf'private.*{method}\(.*?\).*?\{(?:[^{{}}]*|\{(?:[^{{}}]*|\{[^{{}}]*\})*\})*\}', '', content, flags=re.DOTALL)

# 6. Final Cleanups
content = content.replace('using Photon.Pun;', '')
content = content.replace('using System.Reflection;', '')

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)

print("Cleanup complete.")
