const MAX = 48000;
const json = (status, body) => new Response(JSON.stringify(body), {status, headers:{'content-type':'application/json','cache-control':'no-store'}});
export function scrub(text) {
  return text.replace(/\x1b\[[0-?]*[ -/]*[@-~]/g,'')
    .replace(/\b(?:gh[pousr]_[A-Za-z0-9_]+|github_pat_[A-Za-z0-9_]+)\b/g,'[TOKEN]')
    .replace(/(authorization\s*[:=]\s*)([^\r\n]+)/gi,'$1[HIDDEN]')
    .replace(/((?:password|username|token|secret)\s*["']?\s*[:=]\s*)[^\r\n,}]+/gi,'$1[HIDDEN]')
    .replace(/\b(?:\d{1,3}\.){3}\d{1,3}:\d+:[^\s:]+:[^\s]+/g,'[PROXY-CREDENTIALS]')
    .replace(/([A-Z]:\\Users\\)[^\\\s]+/gi,'$1[USER]');
}
export async function handle(request, env, upstream = fetch) {
  try {
    if(new URL(request.url).pathname !== '/reports')return json(404,{error:'not_found'});
    if(request.method !== 'POST')return json(405,{error:'post_required'});
    if(!env.GITHUB_TOKEN || !env.INGEST_KEY || !/^[\w.-]+\/[\w.-]+$/.test(env.DIAGNOSTICS_REPO||''))return json(503,{error:'service_not_configured'});
    if(request.headers.get('authorization') !== 'Bearer '+env.INGEST_KEY)return json(401,{error:'unauthorized'});
    if(!request.headers.get('content-type')?.startsWith('application/json'))return json(415,{error:'json_required'});
    const reader=request.body?.getReader();if(!reader)return json(400,{error:'empty_body'});
    const chunks=[];let length=0;
    while(true){const {done,value}=await reader.read();if(done)break;length+=value.length;if(length>MAX){await reader.cancel();return json(413,{error:'too_large'});}chunks.push(value);}
    const bytes=new Uint8Array(length);let offset=0;for(const c of chunks){bytes.set(c,offset);offset+=c.length;}
    let body;try{body=JSON.parse(new TextDecoder().decode(bytes));}catch{return json(400,{error:'invalid_json'});}
    if(!body || typeof body.log!=='string' || body.log.length>40000 || !/^\d+\.\d+\.\d+$/.test(body.version||''))return json(400,{error:'invalid_report'});
    const headers={'authorization':'Bearer '+env.GITHUB_TOKEN,'user-agent':'NiyazisugarProxy-Diagnostics','accept':'application/vnd.github+json','content-type':'application/json'};
    const base='https://api.github.com/repos/'+env.DIAGNOSTICS_REPO;
    const repo=await upstream(base,{headers});if(!repo.ok)return json(502,{error:'repository_unavailable'});
    if((await repo.json()).private!==true)return json(503,{error:'private_repository_required'});
    const report=scrub(body.log.replaceAll(env.GITHUB_TOKEN,'[SERVER-KEY]').replaceAll(env.INGEST_KEY,'[CONNECTION-CODE]'));if(!report.trim())return json(400,{error:'empty_report'});
    const result=await upstream(base+'/issues',{method:'POST',headers,body:JSON.stringify({title:'NiyazisugarProxy v'+body.version+' — '+new Date().toISOString(),body:'Automatically submitted by the user. Network addresses remain for routing diagnostics.\n\n```text\n'+report.replaceAll('`','´')+'\n```'})});
    if(result.status!==201)return json(502,{error:'github_write_failed'});
    const issue=await result.json();if(!Number.isInteger(issue.number))return json(502,{error:'invalid_receipt'});
    return json(201,{number:issue.number});
  } catch {return json(502,{error:'submission_failed'});}
}
export default {fetch(request,env){return handle(request,env);}};
