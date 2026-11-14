> test info



test suite: `nbomber_default_test_suite_name`

test name: `nbomber_default_test_name`

session id: `2026-11-14_17.24.34_session_a521df98`

> scenario stats



scenario: `get_products`

  - ok count: `71`

  - fail count: `379`

  - all data: `0.0` MB

  - duration: `00:00:14`

load simulations:

  - `inject`, rate: `30`, interval: `00:00:01`, during: `00:00:30`

|step|ok stats|
|---|---|
|name|`global information`|
|request count|all = `450`, ok = `71`, RPS = `5.1`|
|latency (ms)|min = `1.11`, mean = `6.27`, max = `47.82`, StdDev = `8.07`|
|latency percentile (ms)|p50 = `3.22`, p75 = `5.68`, p95 = `20.96`, p99 = `30.78`|
|data transfer (KB)|min = `0.271`, mean = `0.271`, max = `0.271`, all = `0.0` MB|


|step|failures stats|
|---|---|
|name|`global information`|
|request count|all = `450`, fail = `379`, RPS = `27.1`|
|latency (ms)|min = `3.42`, mean = `27.09`, max = `56.46`, StdDev = `10.85`|
|latency percentile (ms)|p50 = `28.29`, p75 = `35.23`, p95 = `43.07`, p99 = `50.82`|


> status codes for scenario: `get_products`



|status code|count|message|
|---|---|---|
|200|71||
|-101|379|Only one usage of each socket address (protocol/network address/port) is normally permitted. (127.0.0.1:5100)|


> scenario stats



scenario: `get_customers`

  - ok count: `1151`

  - fail count: `3332`

  - all data: `9.0` MB

  - duration: `00:00:15`

load simulations:

  - `keep_constant`, copies: `7`, during: `00:00:30`

|step|ok stats|
|---|---|
|name|`global information`|
|request count|all = `4483`, ok = `1151`, RPS = `76.7`|
|latency (ms)|min = `1.01`, mean = `10.41`, max = `99.17`, StdDev = `10.43`|
|latency percentile (ms)|p50 = `7.93`, p75 = `15.11`, p95 = `26.02`, p99 = `52.58`|
|data transfer (KB)|min = `6.34`, mean = `8.001`, max = `13.106`, all = `9.0` MB|


|step|failures stats|
|---|---|
|name|`global information`|
|request count|all = `4483`, fail = `3332`, RPS = `222.1`|
|latency (ms)|min = `2.93`, mean = `24.6`, max = `188.92`, StdDev = `10.52`|
|latency percentile (ms)|p50 = `24.46`, p75 = `31.25`, p95 = `40.1`, p99 = `50.14`|


> status codes for scenario: `get_customers`



|status code|count|message|
|---|---|---|
|200|1151||
|-101|3332|Only one usage of each socket address (protocol/network address/port) is normally permitted. (127.0.0.1:5200)|


> scenario stats



scenario: `register_users`

  - ok count: `21`

  - fail count: `69`

  - all data: `0.0` MB

  - duration: `00:00:14`

load simulations:

  - `inject`, rate: `6`, interval: `00:00:01`, during: `00:00:30`

|step|ok stats|
|---|---|
|name|`global information`|
|request count|all = `90`, ok = `21`, RPS = `1.5`|
|latency (ms)|min = `5.41`, mean = `16.67`, max = `44.49`, StdDev = `10.42`|
|latency percentile (ms)|p50 = `12.4`, p75 = `23.25`, p95 = `32.69`, p99 = `44.51`|
|data transfer (KB)|min = `0.505`, mean = `0.507`, max = `0.509`, all = `0.0` MB|


|step|failures stats|
|---|---|
|name|`global information`|
|request count|all = `90`, fail = `69`, RPS = `4.9`|
|latency (ms)|min = `2.72`, mean = `23.77`, max = `47.7`, StdDev = `10.99`|
|latency percentile (ms)|p50 = `23.89`, p75 = `31.65`, p95 = `41.63`, p99 = `43.17`|


> status codes for scenario: `register_users`



|status code|count|message|
|---|---|---|
|200|21||
|-101|69|Only one usage of each socket address (protocol/network address/port) is normally permitted. (127.0.0.1:5400)|


> scenario stats



scenario: `end_to_end_order`

  - ok count: `6`

  - fail count: `39`

  - all data: `0` MB

  - duration: `00:00:14`

load simulations:

  - `inject`, rate: `3`, interval: `00:00:01`, during: `00:00:30`

|step|ok stats|
|---|---|
|name|`global information`|
|request count|all = `45`, ok = `6`, RPS = `0.4`|
|latency (ms)|min = `229.92`, mean = `242.85`, max = `256.73`, StdDev = `9.29`|
|latency percentile (ms)|p50 = `243.2`, p75 = `249.47`, p95 = `256.77`, p99 = `256.77`|


|step|failures stats|
|---|---|
|name|`global information`|
|request count|all = `45`, fail = `39`, RPS = `2.8`|
|latency (ms)|min = `2.85`, mean = `28.51`, max = `61.03`, StdDev = `13.48`|
|latency percentile (ms)|p50 = `30.59`, p75 = `36.22`, p95 = `49.86`, p99 = `61.06`|


> status codes for scenario: `end_to_end_order`



|status code|count|message|
|---|---|---|
|200|6||
|-101|39|Only one usage of each socket address (protocol/network address/port) is normally permitted. (127.0.0.1:5200)|


> scenario stats



scenario: `mixed_read_burst`

  - ok count: `1384`

  - fail count: `7138`

  - all data: `0` MB

  - duration: `00:00:15`

load simulations:

  - `keep_constant`, copies: `15`, during: `00:00:30`

|step|ok stats|
|---|---|
|name|`global information`|
|request count|all = `8522`, ok = `1384`, RPS = `92.3`|
|latency (ms)|min = `2.7`, mean = `17.68`, max = `108.95`, StdDev = `12.85`|
|latency percentile (ms)|p50 = `16.78`, p75 = `21.02`, p95 = `38.08`, p99 = `70.78`|


|step|failures stats|
|---|---|
|name|`global information`|
|request count|all = `8522`, fail = `7138`, RPS = `475.9`|
|latency (ms)|min = `3`, mean = `24.72`, max = `211.84`, StdDev = `10.74`|
|latency percentile (ms)|p50 = `24.66`, p75 = `31.39`, p95 = `40.16`, p99 = `51.9`|


> status codes for scenario: `mixed_read_burst`



|status code|count|message|
|---|---|---|
|200|1384||
|-101|7138|Only one usage of each socket address (protocol/network address/port) is normally permitted. (127.0.0.1:5200)|


