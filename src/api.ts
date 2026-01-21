import { ApiInterceptorDefinition } from "xmlui";

let id = 1;

function generateId() {
  return id++;
}

function randomUUID() {
  const S4 = function() {
    return (((1+Math.random())*0x10000)|0).toString(16).substring(1);
  };
  return (S4()+S4()+"-"+S4()+"-"+S4()+"-"+S4()+"-"+S4()+S4()+S4());
}

export function createFile(fileName = `Test-file_${generateId()}.txt`, overrides: Record<string, any> = {}) {
  const { driveId, parentId, ...restOverrides } = overrides;
  return {
    id: randomUUID(),
    name: fileName,
    file: {},
    size: Math.floor(Math.random() * (5000 - 100 + 1) + 100),
    createdDateTime: new Date(),
    lastModifiedDateTime: new Date(),
    parentReference: {
      driveId: driveId,
      id: parentId || null,
    },
    ...restOverrides,
  };
}

export function createFolder(fileName = `Test-folder_${generateId()}`, overrides: Record<string, any> = {}) {
  const { driveId, parentId, ...restOverrides } = overrides;
  return {
    id: randomUUID(),
    name: fileName,
    createdDateTime: new Date(),
    lastModifiedDateTime: new Date(),
    folder: {},
    parentReference: {
      driveId: driveId,
      id: parentId || null,
    },
    ...restOverrides,
  };
}

function createUser(displayName: string) {
  const id = randomUUID();
  return {
    id: id,
    displayName: displayName,
  };
}

const johnDoe = createUser("John Doe");
const timTest = createUser("Tim Test");
const testUser = createUser("Test User");
const users = [johnDoe, timTest, testUser];

const testFolder = createFolder("Test-folder", {
  driveId: timTest.id,
});
const files = [
  testFolder,
  createFile("Sample-picture.jpeg", { driveId: timTest.id }),
  createFile("Another-file.pdf", { driveId: timTest.id }),
  createFile("Its an excel file.xlsx", { driveId: timTest.id }),
  createFile("Its another excel file in a subdirectory.xlsx", { driveId: timTest.id, parentId: testFolder.id }),
];

const mock: ApiInterceptorDefinition = {
  type: "<none>",
  apiUrl: "http://localhost:8080/rest",
  config: {
    database: "cbfs-server",
    version: 5,
  },
  schemaDescriptor: {
    tables: [
      {
        name: "files",
        pk: ["&id"],
        indexes: [
          "parentReference.driveId",
          "parentReference.id",
          "[parentReference.driveId+parentReference.id]",
          "[parentReference.driveId+id]",
        ],
      },
      {
        name: "users",
        pk: ["&id"],
      },
      {
        name: "shares",
        indexes: ["itemId"],
        pk: ["&id"],
      },
    ],
  },
  initialData: {
    files: files,
    users: users,
  },
  // auth: {
  //   defaultLoggedInUser: {
  //     id: johnDoe.id,
  //   },
  // },
  // language=JavaScript
  helpers: {
    Converters: {
      // --- Convert a user record to a user DTO
      convertUser: `(user) => (user ? {
                id: user.id,
                displayName: user.displayName
            } : null)`,
      convertUsers: "(users) => users.map(user => Converters.convertUser(user))",
      convertItem: `(item) => {
                const share = $db.$shares.native().where({
                  itemId: item.id
                }).first();
                return {
                    ...item,
                    shared: share ? {
                        owner: {
                            user: Converters.convertUser($db.$users.native().where({id: share.sharedBy}).first())
                        },
                    } : null
                };
            }`,
      convertItems: `(items) => items.map(item => Converters.convertItem(item))`,
      convertShare: `(share) => {
                const file = $db.$files.native().get(share.fileId);
                return {
                    ...share,
                    shareId: share.id,
                    itemInfo: file
                }
            }`,
      convertShares: `(shares) => {
                return shares.map(share => Converters.convertShare(share));
            }`,
    },
    FileRepository: {
      findByIdAndDriveId: `(id, driveId) => {
        const safeDriveId = driveId === 'me' ? $loggedInUser.id : driveId;
        return $db.$files.native().where({
          "parentReference.driveId": safeDriveId,
          "id": id
        }).first();
      }`
    },
    ShareHelpers: {
      getFileId: `(itemId, shareId) => {
                let ret = itemId;
                if (shareId && !Array.isArray(shareId)) {
                    shareId = [shareId];
                }
                if (shareId) {
                    if (shareId.length > 1) {
                        return parseInt(shareId[shareId.length - 1], 10);
                    }
                    const share = $db.$shares.native().get(parseInt(shareId[0], 10));
                    ret = share.fileId;
                }
                return ret;
            }`,
      isSharedByMe: `(itemId) => {
                return $db.$shares.native().where({
                    fileId: itemId,
                    sharedBy: !$loggedInUser ? null : $loggedInUser.id
                }).count() > 0;
            }`,
    },
  },
  operations: {
    "me-read": {
      url: "/me",
      method: "get",
      // language=JavaScript
      handler: `
        // --- The following lines authenticate the wired-in user with index 0
        // --- Comment out these lines to use the default log-in
        const userId = $db.$users.toArray()[0].id;
        $authService.login({id: userId});

        // --- Initial code starts here
        if (!$loggedInUser) {
          return null;
        }
        const user = $db.$users.native().get($loggedInUser.id);
        return Converters.convertUser(user);
      `,
    },
    login: {
      url: "/login",
      method: "post",
      // language=JavaScript
      handler: `
        let id = $queryParams.userId;
        if (!id) {
            id = $db.$users.toArray()[0].id;
        }
        $authService.login({id})
      `,
    },
    logout: {
      url: "/logout",
      method: "post",
      // language=JavaScript
      handler: "$authService.logout()",
    },
    "files-list": {
      url: ["/drives/:driveId/items/:itemId/children"],
      method: "get",
      // language=JavaScript
      handler: `() => {
                const safeDriveId = $pathParams.driveId === 'me' ? $loggedInUser.id : $pathParams.driveId;
                const parentId = $pathParams.itemId === 'root' ? null : $pathParams.itemId;
        //TODO 'shared' property
                let items = $db.$files.toArray().filter((item) => {
                    return item.parentReference.id === parentId && item.parentReference.driveId === safeDriveId;
                });

                return {
                    value: Converters.convertItems(items)
                }
            }`,
    },
    "files-read": {
      url: ["/drives/:driveId/items/:itemId"],
      method: "get",
      // language=JavaScript
      handler: `() => {
          return Converters.convertItem(
              FileRepository.findByIdAndDriveId($pathParams.itemId, $pathParams.driveId)
          );
      }`,
    },
    "files-create": {
      url: ["/drives/:driveId/items/:itemId/children"],
      method: "post",
      // language=JavaScript
      handler: `() => {
                const safeDriveId = $pathParams.driveId === 'me' ? $loggedInUser.id : $pathParams.driveId;
                const parentId = $pathParams.itemId === 'root' ? null : $pathParams.itemId;
                // 'fail', 'replace', 'rename'
                return Converters.convertItem($db.$files.insert({
                    id: crypto.randomUUID(),
                    name: $requestBody.name,
                    folder: $requestBody.folder,
                    parentReference: {
                      driveId: safeDriveId,
                      id: parentId
                    },
                  createdDateTime: Date.now(),
                  lastModifiedDateTime: Date.now()
                }));
            }`,
    },
    "files-delete": {
      url: ["/drives/:driveId/items/:itemId"],
      method: "delete",
      // language=JavaScript
      handler: `() => {
                const safeDriveId = $pathParams.driveId === 'me' ? $loggedInUser.id : $pathParams.driveId;
                $db.$files.native().where({
                  "parentReference.driveId": safeDriveId,
                  "id": $pathParams.itemId
                }).delete();
            }`,
    },
    "files-download": {
      url: ["/drives/:driveId/items/:itemId/content"],
      method: "get",
      // language=JavaScript
      handler: `
                const file = FileRepository.findByIdAndDriveId($pathParams.itemId, $pathParams.driveId);
                
                if (file.contents) {
                    return createFile([file.contents], file.name, {
                        type: file.contents.type
                    })
                }
                return null;
            `,
    },
    "files-download-multiple": {
      url: "/items/content",
      method: "post",
      // language=JavaScript
      handler: `
                //returns an empty zip file with the given filename
                return createFile([], $requestBody.fileName + ".zip", {
                    type: 'application/zip'
                });
            `,
    },
    "files-upload": {
      url: "/drives/:driveId/items/:itemId\\:/:fileName\\:/content",
      method: "put",
      requestShape: "blob",
      // language=JavaScript
      handler: `() => {
         //id with path is in this format: /drives/driveId/items/itemId:/fileName:/content
                const safeDriveId = $pathParams.driveId === 'me' ? $loggedInUser.id : $pathParams.driveId;
                const parentId = $pathParams.itemId === 'root' ? null : $pathParams.itemId;
                const filename = $pathParams.fileName;
                const conflictBehavior = $queryParams['@microsoft.graph.conflictBehavior'];

                const itemWithSameName = $db.$files.toArray().find((file) => file.parentReference.id === parentId && file.parentReference.driveId === safeDriveId && file.name === filename);
                // 'rename', 'fail', 'replace'
                if (conflictBehavior === 'fail' && itemWithSameName) {
                    throw Errors.Conflict409({
                      error: {
                        code: 183,
                        message: 'File already exists'  
                      }
                    });
                }
                if (itemWithSameName) {
                    $db.$files.native().where({
                        "parentReference.driveId": safeDriveId,
                        "id": itemWithSameName.id
                    }).delete();
                }
                
                return $db.$files.insert({
                    id: crypto.randomUUID(),
                    name: filename,
                    parentReference: {
                        driveId: safeDriveId,
                        id: parentId 
                    },
                    size: $requestBody.size,
                    file: {},
                    createdDateTime: Date.now(),
                    lastModifiedDateTime: Date.now(),
                    contents: $requestBody
                });
            }`,
    },
    "files-rename-move": {
      url: ["/drives/:driveId/items/:itemId"],
      method: "patch",
      // language=JavaScript
      handler: `() => {
                const conflictBehavior = $queryParams['@microsoft.graph.conflictBehavior'];
                const {name, parentReference} = $requestBody;
                const safeDriveId = $pathParams.driveId === 'me' ? $loggedInUser.id : $pathParams.driveId;
                const item = FileRepository.findByIdAndDriveId($pathParams.itemId, safeDriveId);
                
                if(!parentReference){           //rename
                  const itemWithSameName = $db.$files.toArray().find((file) =>
                      file.parentReference.id === item.parentReference.id &&
                      file.parentReference.driveId === item.parentReference.driveId &&
                      file.name === name &&
                      file.id !== $pathParams.itemId);
                  // 'fail', 'replace'
                  if (conflictBehavior === 'fail' && itemWithSameName) {
                    throw Errors.Conflict409({
                      error: {
                        code: 183,
                        message: 'File already exists'
                      }
                    });
                  }
                  if (itemWithSameName) {
                    $db.$files.native().where({
                      "parentReference.driveId": safeDriveId,
                      "id": itemWithSameName.id
                    }).delete();
                  }
                  $db.$files.native().where({
                    "parentReference.driveId": safeDriveId,
                    "id": $pathParams.itemId
                  }).modify({
                    name: name
                  });  
                } else {        //move
                  const safeParentId = parentReference.id === 'root' ? null : parentReference.id; 
                  const itemWithSameName = $db.$files.toArray().find((file) =>
                      file.parentReference.id === safeParentId &&
                      file.parentReference.driveId === item.parentReference.driveId &&
                      file.name === name &&
                      file.id !== $pathParams.itemId);
                  
                  if (conflictBehavior === 'fail' && itemWithSameName) {
                    throw Errors.Conflict409({
                      error: {
                        code: 183,
                        message: 'File already exists'
                      }
                    });
                  }
                  if (itemWithSameName) {
                    $db.$files.native().where({
                      "parentReference.driveId": safeDriveId,
                      "id": itemWithSameName.id
                    }).delete();
                  }
                  $db.$files.native().where({
                    "parentReference.driveId": safeDriveId,
                    "id": $pathParams.itemId
                  }).modify({
                    parentReference: {
                        id: safeParentId,
                        driveId: safeDriveId
                    }
                  });
                }
                return FileRepository.findByIdAndDriveId($pathParams.itemId, safeDriveId);
            }`,

    },
    "files-copy": {
      url: ["/drives/:driveId/items/:itemId/copy"],
      method: "post",
      // language=JavaScript
      handler: `
                const conflictBehavior = $queryParams['@microsoft.graph.conflictBehavior'];
                const {parentReference} = $requestBody;
                const safeParentId = parentReference.id === 'root' ? null : parentReference.id;
                const safeDriveId = $pathParams.driveId === 'me' ? $loggedInUser.id : $pathParams.driveId;
                
                const itemToCopy = {...($db.$files.native().where({
                    "parentReference.driveId": safeDriveId,
                    "id": $pathParams.itemId
                  }).first())};
                
                const itemWithSameName = $db.$files.toArray().find((file) =>
                    file.parentReference.id === safeParentId &&
                    file.parentReference.driveId === safeDriveId &&
                    file.name === itemToCopy.name &&
                    file.id !== $pathParams.itemId);
                
                // 'fail', 'replace'
                if (conflictBehavior === 'fail' && itemWithSameName) {
                    throw Errors.Conflict409({
                      error: {
                        code: 183,
                        message: 'File already exists'  
                      }
                    });
                }
                if (itemWithSameName) {
                  $db.$files.native().where({
                    "parentReference.driveId": safeDriveId,
                    "id": itemWithSameName.id
                  }).delete();
                }
                itemToCopy.id = crypto.randomUUID();
                itemToCopy.parentReference = {
                    driveId: safeDriveId,
                    id: safeParentId  
                };
                return $db.$files.insert(itemToCopy);
            `,
    },

    //only for testing purposes (switch logged-in user)
    "users-list": {
      url: "/users",
      method: "get",
      // language=JavaScript
      handler: `return Converters.convertUsers($db.$users.toArray())`,
    },
    "people-list": {
      url: "/me/people",
      method: "get",
      // language=JavaScript
      handler: `return { value: Converters.convertUsers($db.$users.toArray()).map(user => {
          return {
              ...user,
            scoredEmailAddresses: [
                {
                    address: user.id
                }
            ]
          }
        }) }`,
    },
    "permissions-list": {
      url: "/drives/:driveId/items/:itemId/permissions",
      method: "get",
      // language=JavaScript
      handler: `
            const {itemId, driveId} = $pathParams;
            const shares = $db.$shares.toArray().filter(share => share.itemId === itemId && share.driveId === driveId);
            return {
                value: shares.map(share => {
                    return {
                        ...share,
                        shareId: share.id,
                        grantedToIdentities: share.grantedToIdentities.map(id => {
                            return {
                                user: $db.$users.native().where({
                                  id
                                }).first()
                            }
                        })
                    }
                
                })
            };
      `
    },
    "invite-create": {
      url: ["/drives/:driveId/items/:itemId/invite"],
      method: "post",
      // language=JavaScript
      handler: `
                const {itemId, driveId} = $pathParams;
                const {recipients, roles} = $requestBody;

                const share = {
                  id: crypto.randomUUID(),
                  sharedBy: $loggedInUser.id,
                  itemId,
                  driveId,
                  roles,
                  grantedToIdentities: recipients.map(recipient => recipient.email)
                };
                $db.$shares.insert(share);
                return share;
            `,
    },
    "share-link-create": {
      url: '/drives/:driveId/items/:itemId/createLink',
      method: "post",
      // language=JavaScript
      handler: `
            const {itemId, driveId} = $pathParams;
            const {type, scope} = $requestBody;
            
            let roles = [];
            if(type === 'view'){
              roles = ['read'];  
            } else if(type === 'edit'){
              roles = ['write'];  
            } else {
              throw Errors.HttpError(500, {
                  error: {
                      message: 'Unsupported share link type: ' + type
                  }
              });  
            }
            
            const share = {
                id: crypto.randomUUID(),
                sharedBy: $loggedInUser.id,
                itemId,
                driveId,
                roles,
                grantedToIdentities: [],
                link: {
                    type,
                    scope
                }
            };
            $db.$shares.insert(share);
            return {
                ...share,
                shareId: share.id
            };
      `
    } ,
    "shares-list": {
      url: "/me/drive/sharedWithMe",
      method: "get",
      // language=JavaScript
      handler: `
        const shares = $db.$shares.toArray().filter(share => share.grantedToIdentities.includes($loggedInUser.id));
        return {
            value: shares.map(share => ({
                    id: share.id,
                    remoteItem: Converters.convertItem($db.$files.native().where({id: share.itemId, 'parentReference.driveId': share.driveId}).first())
            }))
        }
            `,
    },
    "shares-delete": {
      url: "/drives/:driveId/items/:itemId/permissions/:shareId",
      method: "delete",
      // language=JavaScript
      handler: `
                const {shareId, driveId, itemId} = $pathParams;
                $db.$shares.native().where({
                  id: shareId
                }).delete();
            `,
    },
    "shares-update": {
      url: "/drives/:driveId/items/:itemId/permissions/:shareId",
      method: "patch",
      // language=JavaScript
      handler: `
                const {shareId} = $pathParams;
                $db.$shares.native().where({
                  id: shareId
                }).modify($requestBody);
            `,
    },
    "shares-load": {
      url: "/shares/:shareId/driveItem",
      method: "get",
      // language=JavaScript
      handler: `
                const {shareId} = $pathParams;
                const share = $db.$shares.native().where({
                  id: shareId
                }).first();
                if (!share) {
                    return null;
                }
                return Converters.convertItem($db.$files.native().where({id: share.itemId, 'parentReference.driveId': share.driveId}).first());
            `,
    },
  },
};

export default mock;
