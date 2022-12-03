#import "ElephantController.h"
#import "IdfaConsentViewController.h"
#import <AdSupport/AdSupport.h>
#import <AppTrackingTransparency/AppTrackingTransparency.h>


@implementation ElephantController



void TestFunction(const char * a){
    if(a != nullptr)
        NSLog(@"From Unity -> %s", a);
}

void showForceUpdate(const char * _ttl, const char * _msg) {
     const char * _titleTextCopy = ElephantCopyString(_ttl);
     const char * _messsageTextCopy = ElephantCopyString(_msg);
     
     
     NSString *ttlString = [NSString stringWithCString:_titleTextCopy encoding:NSUTF8StringEncoding];
     NSString *msgString = [NSString stringWithCString:_messsageTextCopy encoding:NSUTF8StringEncoding];
 
     IdfaConsentViewController *viewController = [IdfaConsentViewController sharedInstance];
     [viewController showForceUpdate:ttlString :msgString];
}

void showAlertDialog(const char * _ttl, const char * _msg) {
     const char * _titleTextCopy = ElephantCopyString(_ttl);
     const char * _messsageTextCopy = ElephantCopyString(_msg);
     
     
     NSString *ttlString = [NSString stringWithCString:_titleTextCopy encoding:NSUTF8StringEncoding];
     NSString *msgString = [NSString stringWithCString:_messsageTextCopy encoding:NSUTF8StringEncoding];
 
     IdfaConsentViewController *viewController = [IdfaConsentViewController sharedInstance];
     [viewController showAlertDialog:ttlString :msgString];
 }

void showIdfaConsent(int type, int delay, int position, const char * titleText,
                     const char * descriptionText,
                     const char * buttonText,
                     const char * termsText,
                     const char * policyText,
                     const char * termsUrl,
                     const char * policyUrl) {
    const char * titleTextCopy = ElephantCopyString(titleText);
    const char * descriptionTextCopy = ElephantCopyString(descriptionText);
    const char * buttonTextCopy = ElephantCopyString(buttonText);
    const char * termsTextCopy = ElephantCopyString(termsText);
    const char * policyTextCopy = ElephantCopyString(policyText);
    const char * termsUrlCopy = ElephantCopyString(termsUrl);
    const char * policyUrlCopy = ElephantCopyString(policyUrl);
    
    NSString *title = [NSString stringWithCString:titleTextCopy encoding:NSUTF8StringEncoding];
    NSString *description = [NSString stringWithCString:descriptionTextCopy encoding:NSUTF8StringEncoding];
    NSString *button = [NSString stringWithCString:buttonTextCopy encoding:NSUTF8StringEncoding];
    NSString *terms = [NSString stringWithCString:termsTextCopy encoding:NSUTF8StringEncoding];
    NSString *policy = [NSString stringWithCString:policyTextCopy encoding:NSUTF8StringEncoding];
    NSString *termsUrlString = [NSString stringWithCString:termsUrlCopy encoding:NSUTF8StringEncoding];
    NSString *policyUrlString = [NSString stringWithCString:policyUrlCopy encoding:NSUTF8StringEncoding];
    
    dispatch_async(dispatch_get_main_queue(), ^(void){
        if (@available(iOS 14.0, *)) {
            
            NSString * status = [NSString stringWithUTF8String:getConsentStatus()];
            if ([status isEqualToString:@"NotDetermined"]) {
                IdfaConsentViewController *viewController = [IdfaConsentViewController sharedInstance];
                switch (type) {
                    case 0:
                        [viewController requstPermissionForAppTracking];
                        break;
                    case 1:
                        [viewController presentConsentView];
                        break;
                    case 2:
                        [viewController presentConsentView2];
                        break;
                    case 3:
                        [viewController presentConsentView3:title :delay :position :description :button :terms :policy :termsUrlString :policyUrlString];
                        break;
                    default:
                        [viewController requstPermissionForAppTracking];
                        break;
                }
            } else {
                UnitySendMessage("Elephant", "triggerConsentResult", status.UTF8String);
            }
        } else {
            UnitySendMessage("Elephant", "triggerConsentResult", "");
        }
    });
}

const char* IDFA(){
    NSString *emptyUserIdfa = @"00000000-0000-0000-0000-000000000000";
    NSUUID *u = [[ASIdentifierManager sharedManager] advertisingIdentifier];
    const char *a = [[u UUIDString] cStringUsingEncoding:NSUTF8StringEncoding];
    if ([emptyUserIdfa isEqualToString:[NSString stringWithUTF8String:a]]) {
        return ElephantCopyString("");
    } else {
        return ElephantCopyString(a);
    }
}

const char* getBuildNumber() {
    NSDictionary *infoDict = [[NSBundle mainBundle] infoDictionary];
    NSString *buildNumber = [infoDict objectForKey:@"CFBundleVersion"];
    
    return ElephantCopyString(buildNumber.UTF8String);
}

const char* getConsentStatus(){
    NSString *statusText = @"NotDetermined";
    if (@available(iOS 14.0, *)) {
       ATTrackingManagerAuthorizationStatus status = [ATTrackingManager trackingAuthorizationStatus];
       switch (status) {
           case ATTrackingManagerAuthorizationStatusAuthorized:
               statusText = @"Authorized";
               break;
           case ATTrackingManagerAuthorizationStatusDenied:
               statusText = @"Denied";
               break;
           case ATTrackingManagerAuthorizationStatusRestricted:
               statusText = @"Restricted";
               break;
           case ATTrackingManagerAuthorizationStatusNotDetermined:
               statusText = @"NotDetermined";
               break;
           default:
               statusText = @"NotDetermined";
               break;
       }
    }
    return ElephantCopyString(statusText.UTF8String);
}

void ElephantPost(const char * _url, const char * _body, const char * _gameID, const char * _authToken, int tryCount){


    
            const char * url1 = ElephantCopyString(_url);
            const char * body1 = ElephantCopyString(_body);
            const char * gameID1 = ElephantCopyString(_gameID);
            const char * authToken1 = ElephantCopyString(_authToken);
            
            NSString *urlSt = [NSString stringWithCString:url1 encoding:NSUTF8StringEncoding];
            NSString *body = [NSString stringWithCString:body1 encoding:NSUTF8StringEncoding];
            NSString *gameID = [NSString stringWithCString:gameID1 encoding:NSUTF8StringEncoding];
            NSString *authToken = [NSString stringWithCString:authToken1 encoding:NSUTF8StringEncoding];
            
            NSError *error;

            NSURL *url = [NSURL URLWithString:urlSt];
            NSMutableURLRequest *request = [NSMutableURLRequest requestWithURL:url
                                                                   cachePolicy:NSURLRequestUseProtocolCachePolicy
                                                               timeoutInterval:300.0];

            [request addValue:@"gzip" forHTTPHeaderField:@"Content-Encoding"];
            [request addValue:@"application/json; charset=utf-8" forHTTPHeaderField:@"Content-Type"];
            [request addValue:authToken forHTTPHeaderField:@"Authorization"];
            [request addValue:gameID forHTTPHeaderField:@"GameID"];

            [request setHTTPMethod:@"POST"];

            NSData *requestBodyData = [body dataUsingEncoding:NSUTF8StringEncoding];
            [request setHTTPBody:requestBodyData];


            NSURLSessionDataTask *postDataTask = [[NSURLSession sharedSession] dataTaskWithRequest:request completionHandler:^(NSData *data, NSURLResponse *response, NSError *error) {
                bool failed = false;
                if(error != nil){
                    failed = true;
                }
                else if ([response isKindOfClass:[NSHTTPURLResponse class]]){
                    NSHTTPURLResponse *r = (NSHTTPURLResponse*)response;
                    if(r.statusCode != 200){
                        failed = true;
                    }
                }
                
                if(failed){
                    NSDictionary *failedReq = @{ @"url": urlSt, @"data": body, @"tryCount": [NSNumber numberWithInt:tryCount] };
                            NSData *jsonData = [NSJSONSerialization dataWithJSONObject:failedReq options:NSJSONWritingPrettyPrinted error:nil];
                            NSString *jsonSt = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
                            UnitySendMessage("Elephant", "FailedRequest", [jsonSt cStringUsingEncoding:NSUTF8StringEncoding]);
                } else {
                   if (data != nil &&  data.length > 0) {
                       NSData *jsonData = data;
                       NSString *jsonSt = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
                       UnitySendMessage("RLAdvertisementManager", "SuccessResponse", [jsonSt cStringUsingEncoding:NSUTF8StringEncoding]);
                   }
                   
               }
                
            }];

            [postDataTask resume];
            
            
            
            free((void*)url1);
            free((void*)body1);
            free((void*)gameID1);
            free((void*)authToken1);
   
}


@end
