import { check } from 'k6';
import { Options } from 'k6/options';
import http from 'k6/http';
import { base_url } from './constants';


export let options: Options = {
    vus: 100,
    duration: '60s'
};


export var create_reminder = () => {
    const url = `${base_url}/HttpReminders`
    const req_body = {
        "name": `TEST0011.${__VU}.${__ITER}`,
        "schedule": "2021-08-05T06:40:00.000Z",
        "command": {
            "url": "http://stub.dca.svc.cluster.local/wait/200",
            "httpMethod": "GET"
        }
    };
    const params = {
        headers: {
            'Content-Type': 'application/json'
        }
    };
    const res = http.post(url, JSON.stringify(req_body), params);
    check(res, {
        'status is 201': () => res.status === 201,
    });
};
